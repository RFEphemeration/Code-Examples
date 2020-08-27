using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;

[System.Serializable]
public enum EventType
{
	TurnStart,
	TurnEnd,
	StoneSwap,
	Attack
}

public struct MessageContext
{
	Entity initiator;
	Dictionary<string, object> parameters;

	private static MessageContext _nullContext = new MessageContext ();
	public static MessageContext NullContext { get { return _nullContext; } }

	public bool HasParameter<T>(string name)
	{
		if (!parameters.ContainsKey(name)) {
			return false;
		}
		try {
			T casted = (T)parameters[name];
			return true;
		} catch (InvalidCastException){
			return false;
		}
	}

	public static T Cast <T>(object o)
	{
		return (T) o;
	}

	public static object DynamicCast(object o, Type t)
	{
		MethodInfo castMethod = typeof(MessageContext).GetMethod("Cast").MakeGenericMethod(t);
		return castMethod.Invoke(null, new object[] { o });
	}

	// RMF TODO: is there a better way of passing arguments?
	public T GetParameter<T>(string name, T defaultValue = default(T))
	{
		if (!parameters.ContainsKey(name)) {
			return default(T);
		}

		try {
			return (T)parameters[name];
		} catch (InvalidCastException){
			return default(T);
		}
	}
}



public class EventManager : Singleton<EventManager>
{
	
	public const float kfDefaultPriority = 100f;

	private struct ResponseListener
	{
		public float priority;
		public Predicate<MessageContext> predicate;
		public Action<MessageContext> receiver;

		public ResponseListener(
			float priority,
			Predicate<MessageContext> predicate,
			Action<MessageContext> receiver)
		{
			this.priority = priority;
			this.predicate = predicate;
			this.receiver = receiver;
		}
	}

	private struct EventTrigger
	{
		public EventType eventType;
		public MessageContext context;

		public EventTrigger(EventType eventType, MessageContext context)
		{
			this.eventType = eventType;
			this.context = context;
		}
	}

	private static bool AlwaysRespond(MessageContext context)
	{
		return true;
	}

	private static MethodInfo GetMethodInfo(Action action)
	{
		return action.Method;
	}


	private Dictionary<EventType, List<ResponseListener>> listeners;

	private Queue<EventTrigger> queuedEvents;

	private bool currentlyBroadcasting;

	public void RegisterForMessages(
		EventType eventType,
		Action<MessageContext> receiver)
	{
		RegisterForMessages (eventType, receiver, AlwaysRespond, kfDefaultPriority);
		//Delegate d = this.RegisterForMessages;
		//this.GetType().GetMethod(this.RegisterForMessages);
		MakeMessageReceiver<EventType, Action<MessageContext>>(this.RegisterForMessages);
	}

	/*
	public void RegisterForMessages(EventType eventType,
									 object target,
									 MethodInfo method) {
		RegisterForMessages (eventType, MakeMessageReceiver (target, method), AlwaysRespond, kfDefaultPriority);
	}
	*/

	public void RegisterForMessages(
		EventType eventType,
		Action<MessageContext> receiver,
		Predicate<MessageContext> predicate,
		float priority = kfDefaultPriority)
	{
		if (!listeners.ContainsKey(eventType)) {
			listeners.Add(eventType, new List<ResponseListener>());
		}
		ResponseListener response = new ResponseListener(priority, predicate, receiver);
		if (listeners[eventType][listeners[eventType].Count - 1].priority <= priority) {
			listeners[eventType].Add(response);
		} else {
			// RMF TODO: this is a slow insert operation
			for (int i = 0; i < listeners[eventType].Count; ++i) {
				if (listeners[eventType][i].priority > priority) {
					listeners[eventType].Insert(i, response);
					break;
				}
			}
		}
	}

	public Action<MessageContext> MakeMessageReceiver(Action action)
	{
		return InternalMakeMessageReceiver (action.Target, action.Method);
	}

	public Action<MessageContext> MakeMessageReceiver<T>(Action<T> action)
	{
		return InternalMakeMessageReceiver (action.Target, action.Method);
	}

	public Action<MessageContext> MakeMessageReceiver<T1, T2>(Action<T1, T2> action)
	{
		return InternalMakeMessageReceiver (action.Target, action.Method);
	}

	public Action<MessageContext> MakeMessageReceiver<T1, T2, T3>(Action<T1, T2, T3> action)
	{
		return InternalMakeMessageReceiver (action.Target, action.Method);
	}

	public Action<MessageContext> MakeMessageReceiver<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action)
	{
		return InternalMakeMessageReceiver (action.Target, action.Method);
	}

	private Action<MessageContext> InternalMakeMessageReceiver(
		object target,
		MethodInfo method)
	{
		ParameterInfo[] parametersInfo = method.GetParameters();
		Action<MessageContext> receiver = delegate(MessageContext mc) {
			object[] parameters = new object[parametersInfo.Length];
			for (int i = 0; i < parametersInfo.Length; ++i) {
				ParameterInfo parameterInfo = parametersInfo[i];
				try {
					parameters[i] = mc.GetParameter<object>(parameterInfo.Name);
					parameters[i] = MessageContext.DynamicCast(parameters[i], parameterInfo.ParameterType);
				} catch {
					parameters[i] = parameterInfo.DefaultValue;
				}

			}
			foreach(ParameterInfo parameter in parametersInfo) {
				string name = parameter.Name;
			}
			method.Invoke(target, parameters);
		};
		return receiver;
	}

	public void TriggerEvent(EventType triggeredEvent, MessageContext triggeringContext)
	{
		queuedEvents.Enqueue(new EventTrigger(triggeredEvent, triggeringContext));
		if (currentlyBroadcasting) {
			return;
		}
		currentlyBroadcasting = true;
		while (queuedEvents.Any()) {
			List<Action<MessageContext>> responders = new List<Action<MessageContext>>();
			EventTrigger trigger = queuedEvents.Dequeue();
			EventType eventType = trigger.eventType;
			MessageContext context = trigger.context;
			// evaluate all predicates before sending any responses in case responses change predicate conditions
			foreach (ResponseListener listener in listeners[eventType]) {
				if (listener.predicate(context)) {
					responders.Add(listener.receiver);
				}
			}
			foreach(Action<MessageContext> response in responders) {
				response(context);
			}
		}
	}
}

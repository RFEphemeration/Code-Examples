using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Assertions;



public enum EventComponentType
{
	None,
	Global,
	Target,
	Actor
}

public abstract class DynamicEventMessage
{

	public HashSet<EventComponentType> GetParameterComponentTypes()
	{
		if (componentTypes == null) {
			componentTypes = new HashSet<EventComponentType> (parameterComponents.Keys);
		}
		return componentTypes;
	}

	protected DynamicEventMessage(EventParametersComponent_Global globalParameters)
	{
		parameterComponents.Add (EventComponentType.Global, globalParameters);
	}

	public IEventParametersComponent GetParametersComponent(EventComponentType type)
	{
		IEventParametersComponent component;
		if (!parameterComponents.TryGetValue(type, out component)) {
			return null;
		}
		return component;
	}

	protected Dictionary<EventComponentType, IEventParametersComponent> parameterComponents;

	private HashSet<EventComponentType> componentTypes;
}

public abstract class EventMessage
{
	public virtual bool GetParameter<P>(out P parameter)
		where P : class, IEventParametersComponent
	{
		parameter = null;
		return false;
	}
}

public abstract class EventMessage<P1, P2, P3, P4> : EventMessage
	where P1 : IEventParametersComponent
	where P2 : IEventParametersComponent
	where P3 : IEventParametersComponent
	where P4 : IEventParametersComponent
{
	public readonly P1 parameters1;
	public readonly P2 parameters2;
	public readonly P3 parameters3;
	public readonly P4 parameters4;

	public EventMessage(P1 p1, P2 p2, P3 p3, P4 p4)
	{
		parameters1 = p1;
		parameters2 = p2;
		parameters3 = p3;
		parameters4 = p4;
	}
	public override bool GetParameter<P>(out P parameter)
		where P : class, IEventParametersComponent
	{
		if (typeof(P) == typeof(P1))
			parameters = parameters1;
		else if (typeof(P) == typeof(P2))
			parameters = parameters2;
		else if (typeof(P) == typeof(P3))
			parameters = parameters3;
		else if (typeof(P) == typeof(P4)
			parameters = parameters4;
		else
			parameters = null;
		
		if (parameters == null)
			return false;
		return true;
	}
}

public interface IEventParametersComponent
{
}

public class EventParametersComponent_Null : IEventParametersComponent
{
}

public class EventParametersComponent_Global : IEventParametersComponent
{
	public readonly EventType eventType;

	public EventParametersComponent_Global (EventType eventType)
	{
		this.eventType = eventType;
	}
}

public class EventParametersComponent_Target : IEventParametersComponent
{
	public readonly int targetID;
	public readonly bool singleTarget;

	public EventParametersComponent_Target (int targetID, bool singleTarget)
	{
		this.targetID = targetID;
		this.singleTarget = singleTarget;
	}
}

public class EventParametersComponent_Actor : IEventParametersComponent
{
	public readonly int actorID;

	public EventParametersComponent_Actor (int actorID)
	{
		this.actorID = actorID;
	}
}

// rmf todo: how to force these types to implement IMessageParametersComponent?
public class DynamicEvent_Attack : DynamicEventMessage
{
	DynamicEvent_Attack(
		EventParametersComponent_Actor actorParameters,
		EventParametersComponent_Target targetParameters)
		: base(new EventParametersComponent_Global(EventType.Attack))
	{
		parameterComponents.Add (EventComponentType.Actor, actorParameters);
		parameterComponents.Add (EventComponentType.Target, targetParameters);
	}
}

public class Event_Attack : EventMessage<
	EventParametersComponent_Global,
	EventParametersComponent_Actor, 
	EventParametersComponent_Target,
	EventParametersComponent_Null>
{

	public Event_Attack (
		EventParametersComponent_Global global,
		EventParametersComponent_Actor actor, 
		EventParametersComponent_Target target,
		EventParametersComponent_Null none)
		: base (global, actor, target, none)
	{
		
	}
	
}

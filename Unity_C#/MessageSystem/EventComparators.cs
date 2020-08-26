using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum EventComparatorResult
{
	Accept,
	Neutral,
	Reject
}

/*
public abstract class DynamicEventComparator
{
	protected DynamicEventComparator(EventComparatorComponent_Global globalComparator) {
		comparatorComponents.Add (EventComponentType.Global, globalComparator);
	}

	public HashSet<EventComponentType> GetComparatorComponentTypes() {
		if (componentTypes == null) {
			componentTypes = new HashSet<EventComponentType> (comparatorComponents.Keys);
		}
		return componentTypes;
	}

	public EventComparatorResult EvaluateEventResponse(DynamicEventMessage message) {
		// we must have matching sets of components to be able to respond.
		if (!message.GetParameterComponentTypes ().SetEquals (componentTypes)) {
			return EventComparatorResult.Reject;
		}
		var response = EventComparatorResult.Neutral;

		// rmf note: because we are iterating over the comparator and not the message,
		// a basic comparator could accept a multiparametered message
		// however a complex comparator should not be able to accept a basic message
		foreach (var parameterIter in comparatorComponents) {
			var componentType = parameterIter.Key;
			var comparatorComponent = parameterIter.Value;
			// rmf todo: revisit this and see if it still makes sense
			// having a null component should be equivalent to all of its members being null, which is neutral
			if (comparatorComponent == null) {
				continue;
			}
			var messageComponent = message.GetParametersComponent (componentType);
			// a complex comparator should not be able to accept a basic message
			if (messageComponent == null) {
				response = EventComparatorResult.Reject;
				break;
			}

			var componentResponse = comparatorComponent.EvaluateEventComponent (messageComponent);

			if (componentResponse != EventComparatorResult.Neutral) {
				response = componentResponse;
			}
			if (response == EventComparatorResult.Reject) {
				break;
			}
		}

		return response;
	}

	protected Dictionary<EventComponentType, IEventComparatorComponent> comparatorComponents;

	private HashSet<EventComponentType> componentTypes;
}
*/

public abstract class EventComparator<E, P1, P2, P3, P4, C1, C2, C3, C4>
	where P1 : class, IEventParametersComponent
	where P2 : class, IEventParametersComponent
	where P3 : class, IEventParametersComponent
	where P4 : class, IEventParametersComponent
	where E : EventMessage<P1, P2, P3, P4>
	where C1 : IEventComparatorComponent<P1>
	where C2 : IEventComparatorComponent<P2>
	where C3 : IEventComparatorComponent<P3>
	where C4 : IEventComparatorComponent<P4>
{

	public EventComparator(C1 c1, C2 c2, C3 c3, C4 c4)
	{
		comparator1 = c1;
		comparator2 = c2;
		comparator3 = c3;
		comparator4 = c4;
	}

	public static EventComparatorResult CombineComparatorResults(EventComparatorResult a, EventComparatorResult b)
	{
		if (a == EventComparatorResult.Reject || b == EventComparatorResult.Reject) {
			return EventComparatorResult.Reject;
		} else if (a == EventComparatorResult.Accept || b == EventComparatorResult.Accept) {
			return EventComparatorResult.Accept;
		} else {
			return EventComparatorResult.Neutral;
		}
	}

	readonly C1 comparator1;
	readonly C2 comparator2;
	readonly C3 comparator3;
	readonly C4 comparator4;

	public EventComparatorResult EvaluateEventMessage(E message)
	{
		var response = EventComparatorResult.Neutral;
		var next_response = comparator1.EvaluateEventComponent(message.parameters1);
		if (next_response == EventComparatorResult.Reject)
		{
			return response;
		}
		response = CombineComparatorResults (response, next_response);
		next_response = comparator2.EvaluateEventComponent(message.parameters2);
		if (next_response == EventComparatorResult.Reject)
		{
			return response;
		}
		response = CombineComparatorResults (response, next_response);
		
		next_response = comparator3.EvaluateEventComponent(message.parameters3);
		if (next_response == EventComparatorResult.Reject)
		{
			return response;
		}
		response = CombineComparatorResults (response, next_response);
		
		next_response = comparator4.EvaluateEventComponent(message.parameters4);
		response = CombineComparatorResults (response, next_response);

		return response;
	}
}

public interface IEventComparatorComponent
{
	EventComparatorResult EvaluateEventMessage(EventMessage message);
}

public abstract class IEventComparatorComponent<T> : IEventComparatorComponent where T : class, IEventParametersComponent
{
	public EventComparatorResult EvaluateEventMessage (EventMessage message)
	{
		T parameters;
		bool success = message.GetParameters<T>(out parameters);
		if (!success) {
			return EventComparatorResult.Reject;
		}
		return EvaluateEventComponent(parameters);
	}

	public abstract EventComparatorResult EvaluateEventComponent (T eventComponent);
}

public class EventComparatorComponent_Global : IEventComparatorComponent<EventParametersComponent_Global>
{
	readonly HashSet<EventType> acceptedEventTypes;
	readonly float? responseChance;
	// higher posivite value means go first, 0 is default, lower negative value means go later
	readonly float? responsePriority;

	public EventComparatorComponent_Global(EventType acceptedEventType, float? responseChance, float? responsePriority) {
		this.acceptedEventTypes = new HashSet<EventType> ();
		this.acceptedEventTypes.Add(acceptedEventType);
		this.responseChance = responseChance;
		this.responsePriority = responsePriority;
	}

	public EventComparatorComponent_Global(HashSet<EventType> acceptedEventTypes, float? responseChance, float? responsePriority) {
		this.acceptedEventTypes = acceptedEventTypes;
		this.responseChance = responseChance;
		this.responsePriority = responsePriority;
	}

	/*
	public EventComparatorResult EvaluateEventComponent (IEventParametersComponent eventComponent) {
		if (eventComponent is EventParametersComponent_Global) {
			return EvaluateEventComponent((EventParametersComponent_Global) eventComponent);
		}
		return EventComparatorResult.Reject;
	}
	*/

	public override EventComparatorResult EvaluateEventComponent (EventParametersComponent_Global eventComponent)
	{
		var result = EventComparatorResult.Neutral;
		if (acceptedEventTypes == null) {
			result = EventComparatorResult.Neutral;
		} else if (acceptedEventTypes.Contains (eventComponent.eventType)) {
			result = EventComparatorResult.Accept;
		} else {
			result = EventComparatorResult.Reject;
		}

		if (responseChance.HasValue) {
			// rmf todo: what kind of random?
			if (UnityEngine.Random.value > responseChance) {
				return EventComparatorResult.Reject;
			}
		}

		return result;
	}
}
	
public abstract class EventComparatorComponent_Character<T> : IEventComparatorComponent<T> where T : class, IEventParametersComponent
{
	const int tempSelfCharacterID = 0;
	readonly bool? self;

	public EventComparatorComponent_Character(bool? self) {
		this.self = self;
	}

	/*
	public abstract EventComparatorResult EvaluateEventComponent (IEventParametersComponent eventComponent);
	*/

	public override EventComparatorResult EvaluateEventComponent (T eventComponent)
	{
		int character = GetCharacterFromEventComponent (eventComponent);
		if (self.HasValue) {
			if (character == tempSelfCharacterID) {
				return EventComparatorResult.Accept;
			} else {
				return EventComparatorResult.Reject;
			}
		} else {
			return EventComparatorResult.Neutral;
		}
	}

	protected abstract int GetCharacterFromEventComponent(T eventComponent);
}

public class EventComparatorComponent_Actor : EventComparatorComponent_Character<EventParametersComponent_Actor>
{

	public EventComparatorComponent_Actor (bool? self)
		: base (self)
	{
	}

	/*
	public override EventComparatorResult EvaluateEventComponent (IEventParametersComponent eventComponent) {
		if (eventComponent is EventParametersComponent_Actor) {
			return EvaluateEventComponent((EventParametersComponent_Actor) eventComponent);
		}
		return EventComparatorResult.Reject;
	}
	*/

	protected override int GetCharacterFromEventComponent(EventParametersComponent_Actor eventComponent) {
		return eventComponent.actorID;
	}
}

public class EventComparatorComponent_Target : EventComparatorComponent_Character<EventParametersComponent_Target>
{
	readonly bool? singleTarget;

	public EventComparatorComponent_Target(bool? self, bool? singleTarget)
		: base (self)
	{
		this.singleTarget = singleTarget;
	}

	/*
	public override EventComparatorResult EvaluateEventComponent (IEventParametersComponent eventComponent) {
		if (eventComponent is EventParametersComponent_Target) {
			return EvaluateEventComponent((EventParametersComponent_Target) eventComponent);
		}
		return EventComparatorResult.Reject;
	}
	*/

	public override EventComparatorResult EvaluateEventComponent (EventParametersComponent_Target eventComponent) {
		EventComparatorResult parentResult = base.EvaluateEventComponent (eventComponent);
		if (parentResult == EventComparatorResult.Reject) {
			return parentResult;
		}

		if (singleTarget.HasValue) {
			if (singleTarget.Value == eventComponent.singleTarget) {
				return EventComparatorResult.Accept;
			} else {
				return EventComparatorResult.Reject;
			}
		} else {
			return parentResult;
		}
	}

	protected override int GetCharacterFromEventComponent(EventParametersComponent_Target eventComponent) {
		return eventComponent.targetID;
	}
}


/*
public class DynamicEventComparator_Basic : DynamicEventComparator
{
	DynamicEventComparator_Basic(
		EventComparatorComponent_Global globalParameters)
		: base(globalParameters)
	{
	}
}

public class DynamicEventComparator_Attack : DynamicEventComparator
{
	DynamicEventComparator_Attack(
		EventComparatorComponent_Global globalComparator,
		EventComparatorComponent_Actor actorComparator,
		EventComparatorComponent_Target targetComparator)
		: base(globalComparator)
	{
		comparatorComponents.Add (EventComponentType.Actor, actorComparator);
		comparatorComponents.Add (EventComponentType.Target, targetComparator);
	}
}
*/

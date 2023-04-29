using System.Collections;
using System.Collections.Generic;
using System;

public enum ComparatorResult
{
	Accept,
	Neutral,
	Reject
}

public static class ComparatorResultExtensions
{
	public static ComparatorResult Combine(this ComparatorResult a, ComparatorResult b)
	{
		if (a == ComparatorResult.Reject || b == ComparatorResult.Reject)
		{
			return ComparatorResult.Reject;
		}
		else if (a == ComparatorResult.Accept || b == ComparatorResult.Accept)
		{
			return ComparatorResult.Accept;
		}
		else
		{
			return ComparatorResult.Neutral;
		}
	}
}

public interface IComparatorComponent
{
	public abstract ComparatorResult Evaluate(Event message);
}

public class Comparator
{
	readonly IComparatorComponent[] comparators;

	public Comparator(params IComparatorComponent[] comparators)
	{
		this.comparators = comparators;
	}

	// rmf note: could consider passing game state through here for comparisons
	public ComparatorResult Evaluate(Event message)
	{
		var response = ComparatorResult.Neutral;
		foreach (var comparator in comparators)
		{
			response = response.Combine(comparator.Evaluate(message));
			// rmf note: could consider early-return for Reject
		}
		return response;
	}
}

public abstract class IComparatorComponent<T> : IComparatorComponent where T : class, IEventComponent
{
	public static Type EventComponentType { get { return typeof(T); } }

	public virtual ComparatorResult Evaluate (Event message)
	{
		T component = message.GetComponent<T>();
		if (component == null) {
			return ComparatorResult.Reject;
		}
		return EvaluateComponent(component);
	}

	protected abstract ComparatorResult EvaluateComponent(T eventComponent);
}

public class ComparatorComponent_Null : IComparatorComponent<EventComponent_Null>
{
	public ComparatorComponent_Null() { }

	public override ComparatorResult Evaluate(Event message)
	{
		return ComparatorResult.Neutral;
	}

	protected override ComparatorResult EvaluateComponent(EventComponent_Null eventComponent)
	{
		return ComparatorResult.Neutral;
	}
}

public class ComparatorComponent_Global : IComparatorComponent<EventComponent_Global>
{
	readonly HashSet<EventType> acceptedEventTypes;
	readonly float? responseChance;

	public ComparatorComponent_Global(
		EventType acceptedEventType,
		float? responseChance)
	{
		this.acceptedEventTypes = new HashSet<EventType> ();
		this.acceptedEventTypes.Add(acceptedEventType);
		this.responseChance = responseChance;
	}

	public ComparatorComponent_Global(
		HashSet<EventType> acceptedEventTypes,
		float? responseChance)
	{
		this.acceptedEventTypes = acceptedEventTypes;
		this.responseChance = responseChance;
	}

	protected override ComparatorResult EvaluateComponent(EventComponent_Global eventComponent)
	{
		var result = ComparatorResult.Neutral;
		if (acceptedEventTypes == null) {
			result = ComparatorResult.Neutral;
		} else if (acceptedEventTypes.Contains (eventComponent.eventType)) {
			result = ComparatorResult.Accept;
		} else {
			result = ComparatorResult.Reject;
		}

		if (responseChance.HasValue) {
			if (UnityEngine.Random.value > responseChance) {
				return ComparatorResult.Reject;
			}
		}

		return result;
	}
}
	
public abstract class ComparatorComponent_Character<T> : IComparatorComponent<T> where T : class, IEventComponent
{
	readonly int selfId = 0;
	readonly bool? self;

	public ComparatorComponent_Character(int selfId, bool? self)
	{
		this.selfId = selfId;
		this.self = self;
	}

	protected override ComparatorResult EvaluateComponent(T eventComponent)
	{
		int characterId = GetCharacterFromEventComponent (eventComponent);
		if (self.HasValue) {
			if (self.Value == (characterId == selfId)) {
				return ComparatorResult.Accept;
			} else {
				return ComparatorResult.Reject;
			}
		} else {
			return ComparatorResult.Neutral;
		}
	}

	protected abstract int GetCharacterFromEventComponent(T eventComponent);
}

public class ComparatorComponent_Actor : ComparatorComponent_Character<EventComponent_Actor>
{

	public ComparatorComponent_Actor (int selfId, bool? isActor)
		: base (selfId, isActor)
	{
	}

	protected override int GetCharacterFromEventComponent(
		EventComponent_Actor eventComponent)
	{
		return eventComponent.actorID;
	}
}

// rmf note: could alternatively implement multiple smaller / simpler target comparators
// such as TargetSelf, TargetSingle
public class ComparatorComponent_Target : ComparatorComponent_Character<EventComponent_Target>
{
	readonly bool? singleTarget;

	public ComparatorComponent_Target(int selfId, bool? isTarget, bool? singleTarget)
		: base (selfId, isTarget)
	{
		this.singleTarget = singleTarget;
	}

	protected override ComparatorResult EvaluateComponent(
		EventComponent_Target eventComponent)
	{
		ComparatorResult parentResult = base.EvaluateComponent(eventComponent);
		if (parentResult == ComparatorResult.Reject) {
			return parentResult;
		}

		if (singleTarget.HasValue) {
			if (singleTarget.Value == eventComponent.singleTarget) {
				return ComparatorResult.Accept;
			} else {
				return ComparatorResult.Reject;
			}
		} else {
			return parentResult;
		}
	}

	protected override int GetCharacterFromEventComponent(
		EventComponent_Target eventComponent)
	{
		return eventComponent.targetID;
	}
}

public abstract class ComparatorComponent_Damage : IComparatorComponent<EventComponent_Damage>
{
	readonly int? min;
	readonly int? max;

	public ComparatorComponent_Damage(int? min, int? max)
	{
		this.min = min;
		this.max = max;
	}

	protected override ComparatorResult EvaluateComponent(EventComponent_Damage eventComponent)
	{
		if (min.HasValue)
		{
			if (eventComponent.amount >= min.Value)
			{
				return ComparatorResult.Accept;
			}
			else
			{
				return ComparatorResult.Reject;
			}
		}

		if (max.HasValue)
		{
			if (eventComponent.amount <= max.Value)
			{
				return ComparatorResult.Accept;
			}
			else
			{
				return ComparatorResult.Reject;
			}
		}

		return ComparatorResult.Neutral;
	}
}
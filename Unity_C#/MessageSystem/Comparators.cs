using System.Collections;
using System.Collections.Generic;
using System;

public enum ComparatorResult
{
	Accept,
	Neutral,
	Reject
}

public interface IComparator
{
	public abstract ComparatorResult Evaluate(Event message);
}

public interface IComparatorComponent
{
	public ComparatorResult Evaluate(Event message);
}

public static class Comparator
{
	public static IComparator Make<C1, C2, C3, C4>(C1 c1, C2 c2, C3 c3, C4 c4)
		where C1 : IComparatorComponent
		where C2 : IComparatorComponent
		where C3 : IComparatorComponent
		where C4 : IComparatorComponent
	{
		return new Comparator<C1, C2, C3, C4>(c1, c2, c3, c4);
	}

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

public class Comparator< C1, C2, C3, C4> : IComparator
	where C1 : IComparatorComponent
	where C2 : IComparatorComponent
	where C3 : IComparatorComponent
	where C4 : IComparatorComponent
{
	readonly C1 comparator1;
	readonly C2 comparator2;
	readonly C3 comparator3;
	readonly C4 comparator4;

	public Comparator(C1 c1, C2 c2, C3 c3, C4 c4)
	{
		comparator1 = c1;
		comparator2 = c2;
		comparator3 = c3;
		comparator4 = c4;
	}

	public ComparatorResult Evaluate(Event message)
	{
		var response = ComparatorResult.Neutral;
		response = response.Combine(comparator1.Evaluate(message));
		response = response.Combine(comparator2.Evaluate(message));
		response = response.Combine(comparator3.Evaluate(message));
		response = response.Combine(comparator4.Evaluate(message));
		return response;
	}

	private ComparatorResult EvaluateComponent<C>(C comparator, Event message)
		where C : IComparatorComponent
	{
		return comparator.Evaluate(message);
	}
}

public abstract class IComparatorComponent<T> : IComparatorComponent where T : class, IEventComponent
{
	public static Type EventComponentType { get { return typeof(T); } }

	public virtual ComparatorResult Evaluate (Event message)
	{
		T parameter;
		bool success = message.GetParameter<T>(out parameter);
		if (!success) {
			return ComparatorResult.Reject;
		}
		return EvaluateComponent(parameter);
	}

	public abstract ComparatorResult EvaluateComponent(T eventComponent);
}

public class ComparatorComponent_Null : IComparatorComponent<EventComponent_Null>
{
	public ComparatorComponent_Null() { }

	public override ComparatorResult Evaluate(Event message)
	{
		return ComparatorResult.Neutral;
	}

	public override ComparatorResult EvaluateComponent(EventComponent_Null eventComponent)
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

	public override ComparatorResult EvaluateComponent(EventComponent_Global eventComponent)
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

	public ComparatorComponent_Character(bool? self, int selfId)
	{
		this.self = self;
		this.selfId = selfId;
	}

	public override ComparatorResult EvaluateComponent(T eventComponent)
	{
		int characterId = GetCharacterFromEventComponent (eventComponent);
		if (self.HasValue) {
			if (characterId == selfId) {
				if (self.Value)
				{
					return ComparatorResult.Accept;
				}
				else
				{
					return ComparatorResult.Reject;
				}
			} else {
				if (self.Value)
				{
					return ComparatorResult.Reject;
				}
				else
				{
					return ComparatorResult.Accept;
				}
			}
		} else {
			return ComparatorResult.Neutral;
		}
	}

	protected abstract int GetCharacterFromEventComponent(T eventComponent);
}

public class ComparatorComponent_Actor : ComparatorComponent_Character<EventComponent_Actor>
{

	public ComparatorComponent_Actor (bool? self, int selfId)
		: base (self, selfId)
	{
	}

	protected override int GetCharacterFromEventComponent(
		EventComponent_Actor eventComponent)
	{
		return eventComponent.actorID;
	}
}

public class ComparatorComponent_Target : ComparatorComponent_Character<EventComponent_Target>
{
	readonly bool? singleTarget;

	public ComparatorComponent_Target(bool? self, int selfId, bool? singleTarget)
		: base (self, selfId)
	{
		this.singleTarget = singleTarget;
	}

	public override ComparatorResult EvaluateComponent(
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

	public override ComparatorResult EvaluateComponent(EventComponent_Damage eventComponent)
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
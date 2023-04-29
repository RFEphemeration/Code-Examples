using System.Collections;
using System.Collections.Generic;
using System;

[System.Serializable]
public enum EventType
{
	Attack
}

public interface IEventComponent
{
}

public interface IEvent
{
	public T GetComponent<T>()
		where T : class, IEventComponent;
}

public class Event : IEvent
{
	readonly IEventComponent[] components;

	public Event(params IEventComponent[] components)
	{
		this.components = components;
	}

	// assumes that we won't have multiple components of the same type


	public T GetComponent<T>()
		where T : class, IEventComponent
	{
		foreach (var c in components)
		{
			if (c is T)
			{
				return (T)c;
			}
		}
		return null;
	}
}

public class EventComponent_Null : IEventComponent
{
}

public class EventComponent_Global : IEventComponent
{
	public readonly EventType eventType;

	public EventComponent_Global (EventType eventType)
	{
		this.eventType = eventType;
	}
}

public class EventComponent_Target : IEventComponent
{
	public readonly int targetID;
	public readonly bool singleTarget;

	public EventComponent_Target (int targetID, bool singleTarget)
	{
		this.targetID = targetID;
		this.singleTarget = singleTarget;
	}
}

public class EventComponent_Actor : IEventComponent
{
	public readonly int actorID;

	public EventComponent_Actor (int actorID)
	{
		this.actorID = actorID;
	}
}

public class EventComponent_Damage : IEventComponent
{
	public readonly int amount;

	public EventComponent_Damage (int amount)
	{
		this.amount = amount;
	}
}

public class Event_Attack : Event
{
	public Event_Attack (int actorId, int targetId, int damageAmount)
		: base (
			new EventComponent_Global(EventType.Attack),
			new EventComponent_Actor(actorId),
			new EventComponent_Target(targetId, true),
			new EventComponent_Damage(damageAmount))
	{
	}
}

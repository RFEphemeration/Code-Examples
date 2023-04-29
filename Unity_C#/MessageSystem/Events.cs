using System.Collections;
using System.Collections.Generic;
using System;

[System.Serializable]
public enum EventType
{
	Attack
}

public abstract class Event
{
	public virtual bool GetParameter<P>(out P parameter)
		where P : class, IEventComponent
	{
		parameter = null;
		return false;
	}
}

public abstract class Event<P1, P2, P3, P4> : Event
	where P1 : IEventComponent
	where P2 : IEventComponent
	where P3 : IEventComponent
	where P4 : IEventComponent
{
	public readonly P1 parameters1;
	public readonly P2 parameters2;
	public readonly P3 parameters3;
	public readonly P4 parameters4;

	public Event(P1 p1, P2 p2, P3 p3, P4 p4)
	{
		parameters1 = p1;
		parameters2 = p2;
		parameters3 = p3;
		parameters4 = p4;
	}

	// rmf note: how to enforce P : IEventComponent?
	// assumes that we won't have multiple components of the same type
	public override bool GetParameter<P>(out P parameter)
	{
		if (typeof(P) == typeof(P1))
			parameter = parameters1 as P;
		else if (typeof(P) == typeof(P2))
			parameter = parameters2 as P;
		else if (typeof(P) == typeof(P3))
			parameter = parameters3 as P;
		else if (typeof(P) == typeof(P4))
			parameter = parameters4 as P;
		else
			parameter = null;
		
		if (parameter == null)
			return false;
		return true;
	}
}

public interface IEventComponent
{
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

public class Event_Attack : Event<
	EventComponent_Global,
	EventComponent_Actor, 
	EventComponent_Target,
	EventComponent_Damage>
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

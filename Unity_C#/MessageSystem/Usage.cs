using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Usage : MonoBehaviour
{
	void Start()
	{
		/* rmf note: the intention was to create a 1-1 mapping of event classes
		with comparators for those classes, but the C# type system is limited.
		I believe it would be possible in C++ with template specialization, using
		ComparatorComponent<EventComponent_Global> instead of ComparatorComponent_Global
		Then we could use Comparator<Event_Attack> and do type matching, rather than the EventType enum.
		Not having the 1-1 mapping does allow you to create a wider variety of comparator components,
		and you don't have to give each one a neutral default response, though I have done so
		*/
		var attackMadeByEntityZero = new Comparator(
			new ComparatorComponent_Global(EventType.Attack, null),
			new ComparatorComponent_Actor(selfId: 0, isActor: true),
			new ComparatorComponent_Null(),
			new ComparatorComponent_Null()
		);

		EventManager.Instance.AddListener(attackMadeByEntityZero, this.LogAttackMadeEvent);

		// will log
		EventManager.Instance.BroadcastEvent(new Event_Attack(actorId: 0, targetId: 0, damageAmount: 1));

		// will not log
		EventManager.Instance.BroadcastEvent(new Event_Attack(actorId: 1, targetId: 0, damageAmount: 1));
	}

	void LogAttackMadeEvent(Event e)
	{
		var actor = e.GetComponent<EventComponent_Actor>();
		Debug.Log($"Attack made by {actor.actorID}");
	}
}

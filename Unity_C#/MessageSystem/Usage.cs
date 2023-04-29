using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Usage : MonoBehaviour
{
	// Start is called before the first frame update
	void Start()
	{
		EventManager.Instance = new EventManager();
		var attackMade = Comparator.Make(
			new ComparatorComponent_Global(EventType.Attack, null),
			new ComparatorComponent_Actor(true, 0),
			new ComparatorComponent_Null(),
			new ComparatorComponent_Null()
		);

		EventManager.Instance.AddListener( attackMade, this.PrintEvent );

		EventManager.Instance.BroadcastEvent(new Event_Attack(0, 0, 1));
	}

	// Update is called once per frame
	void Update()
	{
		
	}

	void PrintEvent(Event e)
	{
		EventComponent_Actor actor;
		e.GetParameter(out actor);
		Debug.Log(actor.actorID);
	}
}

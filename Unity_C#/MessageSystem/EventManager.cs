﻿using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

struct Pair<T1, T2>
{
	public T1 first;
	public T2 second;

	public Pair(T1 first, T2 second)
	{
		this.first = first;
		this.second = second;
	}
}

public class EventManager
{
	private static EventManager _Instance;
	public static EventManager Instance
	{
		get
		{
			if (_Instance == null)
			{
				_Instance = new EventManager();
			}
			return _Instance;
		}
	}

	private List<Pair<Comparator, Action<Event>>> listeners = new();

	private Queue<Event> queuedEvents = new();

	public void AddListener(Comparator comparator, Action<Event> response)
	{
		listeners.Add(new Pair<Comparator, Action<Event>>(comparator, response));
	}

	public void BroadcastEvent(Event message)
	{
		queuedEvents.Enqueue(message);
		if (queuedEvents.Count > 1)
		{	
			return;
		}

		while (queuedEvents.Count > 0)
		{
			// rmf note: we don't remove the message until after broadcasting
			// so that resulting messages go to end of the queue instead of interupting
			Event next_message = queuedEvents.Peek();

			foreach (var listener in listeners)
			{
				var result = listener.first.Evaluate(next_message);
				if (result == ComparatorResult.Accept)
				{
					listener.second(next_message);
				}
			}

			queuedEvents.Dequeue();
		}
	}
}
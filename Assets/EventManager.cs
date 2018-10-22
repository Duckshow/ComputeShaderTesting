using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour {
	private static EventManager instance;

	public static EventManager GetInstance() {
		if(instance == null) instance = FindObjectOfType<EventManager>();
		return instance;
	}

	public const int EVENT_COUNT = 9;

	private List<EventOwner> eventOwnersAwakeEarly = new List<EventOwner>();
	private List<EventOwner> eventOwnersAwakeDefault = new List<EventOwner>();
	private List<EventOwner> eventOwnersAwakeLate = new List<EventOwner>();
	private List<EventOwner> eventOwnersStartEarly = new List<EventOwner>();
	private List<EventOwner> eventOwnersStartDefault = new List<EventOwner>();
	private List<EventOwner> eventOwnersStartLate = new List<EventOwner>();
	private List<EventOwner> eventOwnersUpdateEarly = new List<EventOwner>();
	private List<EventOwner> eventOwnersUpdateDefault = new List<EventOwner>();
	private List<EventOwner> eventOwnersUpdateLate = new List<EventOwner>();


	public void AddEventOwner(EventOwner eventOwner) {
		eventOwner.EventIndices = new int[EVENT_COUNT] {
			eventOwnersAwakeEarly.Count,
			eventOwnersAwakeDefault.Count,
			eventOwnersAwakeLate.Count,
			eventOwnersStartEarly.Count,
			eventOwnersStartDefault.Count,
			eventOwnersStartLate.Count,
			eventOwnersUpdateEarly.Count,
			eventOwnersUpdateDefault.Count,
			eventOwnersUpdateLate.Count
		};

		if (eventOwner.IsUsingAwakeEarly()) 	eventOwnersAwakeEarly.		Add(eventOwner);
		if (eventOwner.IsUsingAwakeDefault()) 	eventOwnersAwakeDefault.	Add(eventOwner);
		if (eventOwner.IsUsingAwakeLate()) 		eventOwnersAwakeLate.		Add(eventOwner);
		if (eventOwner.IsUsingStartEarly())		eventOwnersStartEarly.		Add(eventOwner);
		if (eventOwner.IsUsingStartDefault())	eventOwnersStartDefault.	Add(eventOwner);
		if (eventOwner.IsUsingStartLate()) 		eventOwnersStartLate.		Add(eventOwner);
		if (eventOwner.IsUsingUpdateEarly()) 	eventOwnersUpdateEarly.		Add(eventOwner);
		if (eventOwner.IsUsingUpdateDefault()) 	eventOwnersUpdateDefault.	Add(eventOwner);
		if (eventOwner.IsUsingUpdateLate()) 	eventOwnersUpdateLate.		Add(eventOwner);
	}

	public void RemoveEventOwner(EventOwner eventOwner){
		if (eventOwner.IsUsingAwakeEarly()) 	eventOwnersAwakeEarly.		RemoveAt(eventOwner.EventIndices[0]);
		if (eventOwner.IsUsingAwakeDefault()) 	eventOwnersAwakeDefault.	RemoveAt(eventOwner.EventIndices[1]);
		if (eventOwner.IsUsingAwakeLate()) 		eventOwnersAwakeLate.		RemoveAt(eventOwner.EventIndices[2]);
		if (eventOwner.IsUsingStartEarly()) 	eventOwnersStartEarly.		RemoveAt(eventOwner.EventIndices[3]);
		if (eventOwner.IsUsingStartDefault()) 	eventOwnersStartDefault.	RemoveAt(eventOwner.EventIndices[4]);
		if (eventOwner.IsUsingStartLate()) 		eventOwnersStartLate.		RemoveAt(eventOwner.EventIndices[5]);
		if (eventOwner.IsUsingUpdateEarly()) 	eventOwnersUpdateEarly.		RemoveAt(eventOwner.EventIndices[6]);
		if (eventOwner.IsUsingUpdateDefault()) 	eventOwnersUpdateDefault.	RemoveAt(eventOwner.EventIndices[7]);
		if (eventOwner.IsUsingUpdateLate()) 	eventOwnersUpdateLate.		RemoveAt(eventOwner.EventIndices[8]);

		eventOwner.EventIndices = new int[EVENT_COUNT];
	}


	void Awake() {
		for (int i = 0; i < eventOwnersAwakeEarly.Count; i++) eventOwnersAwakeEarly[i].AwakeEarly();
		for (int i = 0; i < eventOwnersAwakeDefault.Count; i++) eventOwnersAwakeDefault[i].AwakeDefault();
		for (int i = 0; i < eventOwnersAwakeLate.Count; i++) eventOwnersAwakeLate[i].AwakeLate();
	}

	void Start() {
		for (int i = 0; i < eventOwnersStartEarly.Count; i++) eventOwnersStartEarly[i].StartEarly();
		for (int i = 0; i < eventOwnersStartDefault.Count; i++) eventOwnersStartDefault[i].StartDefault();
		for (int i = 0; i < eventOwnersStartLate.Count; i++) eventOwnersStartLate[i].StartLate();
	}

	void Update() {
		for (int i = 0; i < eventOwnersUpdateEarly.Count; i++) eventOwnersUpdateEarly[i].UpdateEarly();
		for (int i = 0; i < eventOwnersUpdateDefault.Count; i++) eventOwnersUpdateDefault[i].UpdateDefault();
	}

	void LateUpdate() {
		for (int i = 0; i < eventOwnersUpdateLate.Count; i++) eventOwnersUpdateLate[i].UpdateLate();
	}
}

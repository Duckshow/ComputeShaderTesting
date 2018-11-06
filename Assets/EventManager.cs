using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour {
	private static EventManager instance;

	public static EventManager GetInstance() {
		if(instance == null) instance = FindObjectOfType<EventManager>();
		return instance;
	}

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
		if (eventOwner.IsUsingAwakeEarly()) 	eventOwnersAwakeEarly.		Remove(eventOwner);
		if (eventOwner.IsUsingAwakeDefault()) 	eventOwnersAwakeDefault.	Remove(eventOwner);
		if (eventOwner.IsUsingAwakeLate()) 		eventOwnersAwakeLate.		Remove(eventOwner);
		if (eventOwner.IsUsingStartEarly()) 	eventOwnersStartEarly.		Remove(eventOwner);
		if (eventOwner.IsUsingStartDefault()) 	eventOwnersStartDefault.	Remove(eventOwner);
		if (eventOwner.IsUsingStartLate()) 		eventOwnersStartLate.		Remove(eventOwner);
		if (eventOwner.IsUsingUpdateEarly()) 	eventOwnersUpdateEarly.		Remove(eventOwner);
		if (eventOwner.IsUsingUpdateDefault()) 	eventOwnersUpdateDefault.	Remove(eventOwner);
		if (eventOwner.IsUsingUpdateLate()) 	eventOwnersUpdateLate.		Remove(eventOwner);
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

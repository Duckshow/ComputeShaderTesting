using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// WARNING: probably imcompatible with EventManager
public abstract class Singleton<T> : EventOwner where T : Component {
	private static T instance;

	public static T GetInstance() {
		return instance;
	}

	public static void SetInstance(T newInstance){
		if(instance != null) Debug.LogError("Multiple instances of " + typeof(T).ToString() + "!");
		instance = newInstance;
	}

	protected virtual void Awake() { // not using EventOwner's Awakes bc inheritance
		SetInstance(this as T);
	}
}

using UnityEngine;

public abstract class EventOwner : MonoBehaviour {

	protected virtual void OnEnable() {
		EventManager.GetInstance().AddEventOwner(this);
	}
	protected virtual void OnDisable(){
		EventManager.GetInstance().RemoveEventOwner(this);
	}

	public virtual bool IsUsingAwakeEarly()		{ return false; }
	public virtual bool IsUsingAwakeDefault()	{ return false; }
	public virtual bool IsUsingAwakeLate()		{ return false; }
	public virtual bool IsUsingStartEarly()		{ return false; }
	public virtual bool IsUsingStartDefault()	{ return false; }
	public virtual bool IsUsingStartLate()		{ return false; }
	public virtual bool IsUsingUpdateEarly()	{ return false; }
	public virtual bool IsUsingUpdateDefault()	{ return false; }
	public virtual bool IsUsingUpdateLate()		{ return false; }

	public virtual void AwakeEarly(){ }
	public virtual void AwakeDefault(){ }
	public virtual void AwakeLate(){ }
	public virtual void StartEarly(){ }
	public virtual void StartDefault(){ }
	public virtual void StartLate(){ }
	public virtual void UpdateEarly(){ }
	public virtual void UpdateDefault(){ }
	public virtual void UpdateLate(){ }

	/* TEMPLATE: 
	public override bool IsUsingAwakeEarly() { return false; }
	public override void AwakeEarly() { }

	public override bool IsUsingAwakeDefault() { return false; }
	public override void AwakeDefault() { }

	public override bool IsUsingAwakeLate() { return false; }
	public override void AwakeLate() { }

	public override bool IsUsingStartEarly() { return false; }
	public override void StartEarly() { }

	public override bool IsUsingStartDefault() { return false; }
	public override void StartDefault() { }

	public override bool IsUsingStartLate() { return false; }
	public override void StartLate() { }

	public override bool IsUsingUpdateEarly() { return false; }
	public override void UpdateEarly() { }

	public override bool IsUsingUpdateDefault() { return false; }
	public override void UpdateDefault() { }

	public override bool IsUsingUpdateLate() { return false; }
	public override void UpdateLate(){ }
	*/
}

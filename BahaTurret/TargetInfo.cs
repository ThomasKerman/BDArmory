//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18449
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BahaTurret
{
	public class TargetInfo : MonoBehaviour
	{
		public BDArmorySettings.BDATeams team;
		public bool isMissile = false;

		public MissileLauncher missileModule = null;

		public bool isLanded
		{
			get
			{
				return vessel.LandedOrSplashed;
			}
		}
		public Vector3 velocity
		{
			get
			{
				return vessel.srf_velocity;
			}
		}
		public Vector3 position
		{
			get
			{
				return vessel.transform.position;
			}
		}

		private Vessel vessel;
		public Vessel Vessel
		{
			get
			{
				if(!vessel)
				{
					vessel = GetComponent<Vessel>();
				}

				return vessel;
			}
			set
			{
				vessel = value;
			}
		}

		public bool isThreat
		{
			get
			{
				if(!Vessel)
				{
					return false;
				}
				foreach(var wm in Vessel.FindPartModulesImplementing<MissileFire>())
				{
					return true;
				}
				return false;
			}
		}

		List<MissileFire> friendliesEngaging;

		bool hasStarted = false;

		void Start()
		{
			hasStarted = true;
			if(!vessel)
			{
				vessel = GetComponent<Vessel>();
			}
			if(!vessel)
			{
				Debug.Log ("TargetInfo was added to a non-vessel");
				Destroy (this);
			}

			bool foundMf = false;
			foreach(var mf in vessel.FindPartModulesImplementing<MissileFire>())
			{
				foundMf = true;
				team = mf.team ? BDArmorySettings.BDATeams.B : BDArmorySettings.BDATeams.A;
				break;
			}
			bool foundMl = false;
			if(!foundMf)
			{
				foreach(var ml in vessel.FindPartModulesImplementing<MissileLauncher>())
				{
					foundMl = true;
					team = ml.team ? BDArmorySettings.BDATeams.B : BDArmorySettings.BDATeams.A;
					break;
				}

				if(!foundMl)
				{
					Debug.Log("TargetInfo was added to vessel with mo WpnMgr or MissileLauncher");
					Destroy(this);
				}
			}


			if(!BDATargetManager.TargetDatabase[BDATargetManager.OtherTeam(team)].Contains(this))
			{
				BDATargetManager.TargetDatabase[BDATargetManager.OtherTeam(team)].Add(this);
			}

			friendliesEngaging = new List<MissileFire>();
			vessel.OnJustAboutToBeDestroyed += AboutToBeDestroyed;
		}

		void Update()
		{
			if(!vessel)
			{
				AboutToBeDestroyed();
			}
		}

		public int numFriendliesEngaging
		{
			get
			{
				if(friendliesEngaging == null)
				{
					return 0;
				}
				friendliesEngaging.RemoveAll(item => item == null);
				return friendliesEngaging.Count;
			}
		}

		public void Engage(MissileFire mf)
		{
			if(!hasStarted)
			{
				Start();
			}
			if(!friendliesEngaging.Contains(mf))
			{
				friendliesEngaging.Add(mf);
			}
		}

		public void Disengage(MissileFire mf)
		{
			friendliesEngaging.Remove(mf);
		}
		
		void AboutToBeDestroyed()
		{
			BDATargetManager.TargetDatabase[team].Remove(this);
			Destroy(this);
		}

		public bool IsCloser(TargetInfo otherTarget, MissileFire myMf)
		{
			float thisSqrDist = (position-myMf.transform.position).sqrMagnitude;
			float otherSqrDist = (otherTarget.position-myMf.transform.position).sqrMagnitude;
			return thisSqrDist < otherSqrDist;
		}
	}
}


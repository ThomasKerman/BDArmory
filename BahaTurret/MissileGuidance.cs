using System;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{
	public class MissileGuidance
	{
		
		public static Vector3 GetAirToGroundTarget(Vector3 targetPosition, Vessel missileVessel, Vessel targetVessel, float descentRatio)
		{
			Vector3 upDirection = -FlightGlobals.getGeeForceAtPosition(targetPosition).normalized;
			Vector3 surfacePos = missileVessel.transform.position - ((float)missileVessel.altitude*upDirection);
			Vector3 targetSurfacePos = targetVessel.transform.position - ((float)targetVessel.altitude*upDirection);
			float distanceToTarget = Vector3.Distance(surfacePos, targetSurfacePos);
			
			if(missileVessel.srfSpeed < 75 && missileVessel.verticalSpeed < 10)//gain altitude if launching from stationary
			{
				return missileVessel.transform.position + (5*missileVessel.transform.forward) + (7 * upDirection);	
			}
			
			Vector3 finalTarget = targetPosition +(Mathf.Clamp((distanceToTarget-((float)missileVessel.srfSpeed*descentRatio))*0.22f, 0, 2000) * upDirection);
		

			return finalTarget;

		}

		public static Vector3 GetAirToAirTarget(Vector3 targetPosition, Vessel missileVessel, Vessel targetVessel)
		{
			if(!targetVessel)
			{
				return targetPosition;
			}

			float leadTime = 0;
			float targetDistance = Vector3.Distance(targetVessel.transform.position, missileVessel.transform.position);
			if(targetVessel.rigidbody)
			{
				leadTime = (1/((targetVessel.rigidbody.velocity-missileVessel.rigidbody.velocity).magnitude/targetDistance));
				leadTime = Mathf.Clamp(leadTime, 0f, 5f);
				targetPosition = targetPosition + (targetVessel.rigidbody.velocity*leadTime);
			}
			if(targetDistance < 1600)
			{
				targetPosition += (Vector3)targetVessel.acceleration * 0.025f * Mathf.Pow(leadTime,2);
			}


			Vector3 upDirection = -FlightGlobals.getGeeForceAtPosition(targetPosition).normalized;
			Vector3 surfacePos = missileVessel.transform.position - ((float)missileVessel.altitude*upDirection);
			Vector3 targetSurfacePos = targetVessel.transform.position - ((float)targetVessel.altitude*upDirection);
			float distanceToTarget = Vector3.Distance(surfacePos, targetSurfacePos);

			//Vector3 finalTarget = targetPosition +(Mathf.Clamp((distanceToTarget-800)*0.05f, 0, 250) * upDirection);

			Vector3 finalTarget = targetPosition + Mathf.Clamp((float)(targetVessel.altitude-missileVessel.altitude)/4, 0, 1500)*upDirection;

			return finalTarget;
		}

		public static Vector3 GetAirToAirFireSolution(MissileLauncher missile, Vessel targetVessel)
		{
			if(!targetVessel)
			{
				return missile.transform.position + (missile.transform.forward*1000);
			}
			Vector3 targetPosition = targetVessel.transform.position;
			float leadTime = 0;
			float targetDistance = Vector3.Distance(targetVessel.transform.position, missile.transform.position);
			if(targetVessel.rigidbody)
			{
				Vector3 simMissileVel = missile.optimumAirspeed * (targetPosition-missile.transform.position).normalized;
				leadTime = (1/((targetVessel.rigidbody.velocity-simMissileVel).magnitude/targetDistance));
				targetPosition = targetPosition + (targetVessel.rigidbody.velocity*leadTime);
			}
			if(targetVessel && targetDistance < 800)
			{
				targetPosition += (Vector3)targetVessel.acceleration * 0.05f * Mathf.Pow(leadTime,2);
			}
			
			return targetPosition;
		}

		public static Vector3 GetCruiseTarget(Vector3 targetPosition, Vessel missileVessel, Vessel targetVessel, float radarAlt)
		{
			Vector3 upDirection = -FlightGlobals.getGeeForceAtPosition(missileVessel.GetWorldPos3D()).normalized;
			float currentRadarAlt = GetRadarAltitude(missileVessel);
			float distanceSqr = (targetPosition-(missileVessel.transform.position-(currentRadarAlt*upDirection))).sqrMagnitude;

			float agmThreshDist = 3500;

			Vector3 planarDirectionToTarget = Misc.ProjectOnPlane(targetPosition-missileVessel.transform.position, missileVessel.transform.position, upDirection).normalized;

			if(distanceSqr < agmThreshDist*agmThreshDist)
			{
				return GetAirToGroundTarget(targetPosition, missileVessel, targetVessel, 2.3f);
			}
			else
			{
				if(missileVessel.srfSpeed < 50 && missileVessel.verticalSpeed < 5) //gain altitude if launching from stationary
				{
					return missileVessel.transform.position + (5*missileVessel.transform.forward) + (40 * upDirection);	
				}

				Vector3 tRayDirection = (Misc.ProjectOnPlane(missileVessel.rigidbody.velocity, missileVessel.transform.position, upDirection).normalized * 10) - (10*upDirection);
				Ray terrainRay = new Ray(missileVessel.transform.position, tRayDirection);
				RaycastHit rayHit;
				if(Physics.Raycast(terrainRay, out rayHit, 8000, 1<<15))
				{
					float detectedAlt = Vector3.Project(rayHit.point-missileVessel.transform.position, upDirection).magnitude;

					float error = Mathf.Min(detectedAlt, (float)missileVessel.altitude) - radarAlt;
					error = Mathf.Clamp(0.1f * error, -3, 3);

					return missileVessel.transform.position + (10*planarDirectionToTarget) - (error * upDirection);

				}
				else
				{
					float error = (float)missileVessel.altitude - radarAlt;
					error = Mathf.Clamp(0.1f * error, -3, 3);
					
					return missileVessel.transform.position + (10*planarDirectionToTarget) - (error * upDirection);	
				}

			}
		}

		public static Vector3 GetTerminalManeuveringTarget(Vector3 targetPosition, Vessel missileVessel, Vessel targetVessel)
		{
			Vector3 upDirection = -FlightGlobals.getGeeForceAtPosition(missileVessel.GetWorldPos3D()).normalized;
			Vector3 planarDirectionToTarget = Vector3.ProjectOnPlane(targetPosition-missileVessel.transform.position, upDirection).normalized;
			Vector3 crossAxis = Vector3.Cross(planarDirectionToTarget, upDirection).normalized;
			float sinAmplitude = Mathf.Clamp(Vector3.Distance(targetPosition, missileVessel.transform.position)-550, 0, 2500);
			Vector3 targetSin = (Mathf.Sin(2*Time.time) * sinAmplitude * crossAxis)+targetPosition;
			return GetAirToGroundTarget (targetSin, missileVessel, targetVessel, 6);
		}


		public static FloatCurve DefaultLiftCurve = null;
		public static FloatCurve DefaultDragCurve = null;
		public static Vector3 DoAeroForces(MissileLauncher ml, Vector3 targetPosition, float liftArea, float steerMult, Vector3 previousTorque, float maxTorque, float maxAoA)
		{
			Rigidbody rb = ml.rigidbody;
			double airDensity = ml.vessel.atmDensity;
			double airSpeed = ml.vessel.srfSpeed;

			//temp values
			Vector3 CoL = new Vector3(0, 0, -1f);
			//float liftArea = 0.015f;
			float liftCoefficient = 0.1f;
			//float steerMult = .55f;
			//float maxDeflectionForce = 10;
			//float maxAoA = ml.maxAoA;


			if(DefaultLiftCurve == null)
			{
				DefaultLiftCurve = new FloatCurve();
				DefaultLiftCurve.Add(0, .1f);
				DefaultLiftCurve.Add(8, .55f);
				DefaultLiftCurve.Add(19, 1);
				DefaultLiftCurve.Add(23, 1);
				DefaultLiftCurve.Add(29, 0.85f);
				DefaultLiftCurve.Add(65, .1f);
				DefaultLiftCurve.Add(90, .1f);
			}

			if(DefaultDragCurve == null)
			{
				DefaultDragCurve = new FloatCurve();
				DefaultDragCurve.Add(0, 0);
				DefaultDragCurve.Add(5, -.015f);
				DefaultDragCurve.Add(15, .015f);
				DefaultDragCurve.Add(45, .085f);
				DefaultDragCurve.Add(90, .5f);
			}


			FloatCurve liftCurve = DefaultLiftCurve;
			FloatCurve dragCurve = DefaultDragCurve;



			//lift
			float AoA = Mathf.Clamp(Vector3.Angle(ml.transform.forward, rb.velocity.normalized), 0, 90);
			if(AoA > 0)
			{
				double liftForce = 0.5 * airDensity * Math.Pow(airSpeed, 2) * liftArea * liftCoefficient * liftCurve.Evaluate(AoA);
				Vector3 forceDirection = Vector3.ProjectOnPlane(-rb.velocity, ml.transform.forward).normalized;
				rb.AddForceAtPosition((float)liftForce * forceDirection, ml.transform.TransformPoint(CoL));

				//extra drag
				double dragForce = 0.5 * airDensity * Math.Pow(airSpeed, 2) * liftArea * liftCoefficient * dragCurve.Evaluate(AoA);
				rb.AddForceAtPosition((float)dragForce * -rb.velocity.normalized, ml.transform.TransformPoint(CoL));
			}
			//guidance
			if(airSpeed > 1 && AoA < maxAoA)
			{
				Vector3 targetDirection = (targetPosition-ml.transform.position);
				//debugString += "\nSurface Distance: "+surfaceDistance.ToString("0.0");

				Vector3 torqueDirection = -Vector3.Cross(targetDirection, ml.transform.forward).normalized;
				torqueDirection = ml.transform.InverseTransformDirection(torqueDirection);
				float targetAngle = Vector3.Angle(ml.rigidbody.velocity.normalized, targetDirection);
				float torque = Mathf.Clamp(targetAngle * steerMult, 0, maxTorque);
				Vector3 finalTorque = Vector3.ProjectOnPlane(Vector3.Lerp(previousTorque, torqueDirection*torque, 0.1f), Vector3.forward);

				rb.AddRelativeTorque(finalTorque);

				//anti-spin
				/*
				Vector3 localAngVel = rb.transform.InverseTransformDirection(rb.angularVelocity);
				localAngVel -= localAngVel.z * Vector3.forward;
				rb.angularVelocity = rb.transform.TransformDirection(localAngVel);
				*/

				return finalTorque;
				
			//	Vector3 dragOffsetTarget = Vector3.ClampMagnitude(steerMult * Vector3.ProjectOnPlane(targetDirection, transform.forward), maxSteer);
			//	dragOffset = Vector3.MoveTowards(dragOffset, dragOffsetTarget, 60*Time.fixedDeltaTime);
			}
			else
			{
				Vector3 finalTorque = Vector3.ProjectOnPlane(Vector3.Lerp(previousTorque, Vector3.zero, 0.1f), Vector3.forward);
				rb.AddRelativeTorque(finalTorque);
				return finalTorque;
			}
		}

		public static float GetRadarAltitude(Vessel vessel)
		{
			float radarAlt = Mathf.Clamp((float)(vessel.mainBody.GetAltitude(vessel.findWorldCenterOfMass())-vessel.terrainAltitude), 0, (float)vessel.altitude);
			return radarAlt;
		}

		public static float GetRaycastRadarAltitude(Vector3 position)
		{
			Vector3 upDirection = -FlightGlobals.getGeeForceAtPosition(position).normalized;
			Ray ray = new Ray(position, -upDirection);
			float rayDistance = FlightGlobals.getAltitudeAtPos(position);

			if(rayDistance < 0)
			{
				return 0;
			}

			RaycastHit rayHit;
			if(Physics.Raycast(ray, out rayHit, rayDistance, 1<<15)) 
			{
				return Vector3.Distance(position, rayHit.point);
			}
			else
			{
				return rayDistance;
			}
		}
	}
}


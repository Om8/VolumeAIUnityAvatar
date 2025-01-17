﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


enum EyeState
{
	interacting,
	idle, 
	thinking
}

namespace AI.Volume.Bot.Visual
{
	public class HeadRotator : MonoBehaviour
	{
		//Both eye transforms, used to look at player.
		[Header("Bones")]
		[Tooltip("Characters left eye"), CannotBeNullObjectField]
		public Transform leftEye = null;
		[Tooltip("Characters right eye"), CannotBeNullObjectField]
		public Transform rightEye = null;
		[CannotBeNullObjectField]
		public Transform headRoot = null;


		[Header("Idle Eyes")]
		//Timer for Idle eyes
		[SerializeField, Tooltip("Max time between the eyes move a small distance")]
		private float maxTimeSmallJump = 2;
		[SerializeField, Tooltip("Min time between the eyes moving to a new small distance")]
		private float minTimeSmallJump = .7f;
		[SerializeField, Tooltip("Min time between a big jump")]
		private int minTimesLookAround = 2;
		[SerializeField, Tooltip("Max time between big jump")]
		private int maxTimeLookAround = 5;
		private float currentTime;
		private int currentLook;

		[Header("Big Eye Jumps")]
		[SerializeField, Tooltip("Distance in front of bot that the eyes should look")]
		private float lookDist = 3;
		[SerializeField, Tooltip("Size of circle that the end of the cone will be")]
		private float lookAngle = .5f;
		[Header("Small Eye Jumps")]
		[SerializeField, Tooltip("Distance in front of bot that the eyes should look")]
		private float smallLookDist = 3;
		[SerializeField, Tooltip("Size of circle that the end of the cone will be")]
		private float smallLookAngle = .2f;

		[Header("General Eyes")]
		[SerializeField, Tooltip("The lerp speed of the eyes")]
		private float eyeLerpSpeed = 50;

		[Header("Interaction Eyes")]
		[SerializeField, Tooltip("The size of the users face, how wide can the avatar look?")]
		private float faceSize = 1;
		[SerializeField, Tooltip("The maximum time between eye jumps around the face")]
		private float interactionMaxTime = .9f;
		[SerializeField, Tooltip("The minumum time between eye jumps around the face")]
		private float interactionMinTime = .2f;
		Vector3 interactionOffsetPosition;


		[Header("Head")]
		[SerializeField, Tooltip("Max direction between eyes and head before the head aligns to the eyes")]
		private float headRotationThreshold = 30;
		[SerializeField, Tooltip("How fast the head rotates")]
		private float headLerpSpeed = 2;
		[SerializeField, Tooltip("Lock the head to an angle from forwards")]
		private float maxTurnAngle = 50;

		private Vector3 lerpingHeadLookPosition;
		private Vector3 headLookPosition;

		private Vector3 lookAtPosition;
		private Vector3 lerpingValue;

		private Quaternion currentLookRotation;

		private EyeState stateOfEyes = EyeState.idle;

		[SerializeField, CannotBeNullObjectField]
		private HeadBob headBobScript = null;
		private Vector3 initialHeadStartOffset;
		private Vector3 desiredHeadPosition;
		[SerializeField]
		private float maxHeadSway = 1;

		[Header("Thinking Eyes")]
		[SerializeField, Tooltip("When thinking, how far to the side can the eyes possibly be")]
		private float thinkOffsetSide = 1;
		[SerializeField, Tooltip("How far up can the eyes go when thinking")]
		private float maxThinkOffsetUp = 1;
		[SerializeField, Tooltip("Minimum that the eyes can go up when thinking")]
		private float minThinkOffsetUp = .2f;
		private Vector3 thinkingOffset;

		private Quaternion currentOffset;

		private void Start()
		{
			if (leftEye != null)
			{
				//Set up values on start
				lerpingValue = Camera.main.transform.position;
				headLookPosition = lerpingValue;
				lerpingHeadLookPosition = headLookPosition;
			}
			if (headRoot != null)
			{
				initialHeadStartOffset = headRoot.localPosition;
				desiredHeadPosition = initialHeadStartOffset;
			}
			currentLook = Random.Range(minTimesLookAround, maxTimeLookAround);

		}

		private void Update()
		{
			//Test the thinking.
			if (Input.GetKeyDown(KeyCode.G))
			{
				StartedThinking();
			}
		}

		// Update is called once per frame
		private void LateUpdate()
		{
			switch (stateOfEyes)
			{
				case EyeState.idle:
					CountDownToChangeDirection();

					//Set eye rotations on update
					if (leftEye != null) SetEyeRotation(leftEye);
					if (rightEye != null) SetEyeRotation(rightEye);
					HeadFollowEyes();
					break;
				case EyeState.interacting:
					InteractingEyes();

					//Set eye rotations on update
					if (headRoot != null) SetInteractingHeadPosition();
					if (leftEye != null) SetEyeRotation(leftEye);
					if (rightEye != null) SetEyeRotation(rightEye);

					break;
				case EyeState.thinking:
					//Set thinking eyes.
					if (leftEye != null) SetEyeRotation(leftEye);
					if (rightEye != null) SetEyeRotation(rightEye);
					if (headRoot != null) HeadFollowEyes();
					break;
			}

		}

		/// <summary>
		/// Get a random position in front of the head and set the look position
		/// </summary>
		private void IdleEyes(float lookDistance, float lookAng)
		{
			lookAtPosition = GetPositionInCome(headRoot.transform.position, this.transform.forward, lookDistance, lookAng);
		}



		/// <summary>
		/// Cool down for eyes, set the min and max time to adjust the speed.
		/// </summary>
		private void CountDownToChangeDirection()
		{
			currentTime = Mathf.Clamp(currentTime - Time.deltaTime, 0, maxTimeSmallJump);
			if (currentTime == 0)
			{
				if (currentLook > 0)
				{
					currentTime = Random.Range(minTimeSmallJump, maxTimeSmallJump);
					IdleEyes(smallLookDist, smallLookAngle);
					currentLook = Mathf.Clamp(currentLook - 1, 0, maxTimeLookAround);
				}
				else
				{
					currentLook = Random.Range(minTimesLookAround, maxTimeLookAround);
					currentTime = Random.Range(minTimeSmallJump, maxTimeSmallJump);
					IdleEyes(lookDist, lookAngle);
				}

			}
		}

		/// <summary>
		/// When the bot has been interacted with, look at the player
		/// </summary>
		private void InteractingEyes()
		{
			GetInteractionEyePosition();
			if (Camera.main != null)
			{
				lookAtPosition = Camera.main.transform.position + interactionOffsetPosition;
			}
		}

		/// <summary>
		/// Once the player has asked a question, do a thinking pose.
		/// </summary>
		private void ThinkingEyes()
		{

		}


		/// <summary>
		/// Get the eye position of the eyes when interacting. 
		/// </summary>
		private void GetInteractionEyePosition()
		{
			currentTime = Mathf.Clamp(currentTime - Time.deltaTime, 0, maxTimeSmallJump);
			if (currentTime == 0)
			{
				if (Camera.main != null)
				{
					interactionOffsetPosition = Random.insideUnitSphere * faceSize;
				}
				currentTime = Random.Range(interactionMinTime, interactionMaxTime);

			}

		}

		//Adjust the head positions
		private void SetInteractingHeadPosition()
		{
			if (headRoot != null && Camera.main != null)
			{
				SetFinalHeadRotation();
			}
		}

		/// <summary>
		///  Works out vector direction to look and sets the eye transform to look at it
		/// </summary>
		/// <param name="eye">Eye socket transform to rotate</param>
		private void SetEyeRotation(Transform eye)
		{
			lerpingValue = Vector3.Lerp(lerpingValue, lookAtPosition, Time.deltaTime * eyeLerpSpeed);
			eye.rotation = Quaternion.LookRotation((lerpingValue - eye.position).normalized, Vector3.up);
		}
		//Fix this later, this currently does not work properly.

		/// <summary>
		/// Player has started interacting with the bot.
		/// </summary>
		public void StartedInteracting()
		{
			stateOfEyes = EyeState.interacting;
		}

		public void StartedThinking()
		{
			stateOfEyes = EyeState.thinking;
			if (Camera.main != null)
			{
				lookAtPosition = Camera.main.transform.position + new Vector3(Random.Range(-thinkOffsetSide, thinkOffsetSide), Random.Range(minThinkOffsetUp, maxThinkOffsetUp));
			}
		}

		/// <summary>
		/// Has started idling.
		/// </summary>
		public void StartedIdling()
		{
			stateOfEyes = EyeState.idle;
		}

		//Follows the eyes if the eyes move out of threshold.
		private void HeadFollowEyes()
		{
			if (headRoot != null && leftEye != null)
			{
				if (Vector3.Angle(headRoot.forward, leftEye.forward) > headRotationThreshold)
				{
					headLookPosition = lookAtPosition;
				}
				SetFinalHeadRotation();
			}
		}

		//Rotate head within the clamped range. 
		private void SetFinalHeadRotation()
		{
			lerpingHeadLookPosition = Vector3.Lerp(lerpingHeadLookPosition, lookAtPosition, Time.deltaTime * headLerpSpeed);
			headRoot.rotation = Quaternion.LookRotation((lerpingHeadLookPosition - headRoot.position).normalized, Vector3.up);
			if (headBobScript != null)
			{
				Vector3 ourRotation = headRoot.rotation.eulerAngles;
				Vector3 combinedRotation = ourRotation + headBobScript.bobDirection;

				desiredHeadPosition = Vector3.Lerp(desiredHeadPosition, (headBobScript.headOffset * maxHeadSway) + initialHeadStartOffset, Time.deltaTime * 2);
				headRoot.rotation = Quaternion.Euler(combinedRotation);
				headRoot.rotation = Quaternion.Euler(headRoot.transform.eulerAngles.x, Mathf.Clamp(headRoot.transform.eulerAngles.y, headRoot.parent.transform.eulerAngles.y - maxTurnAngle, headRoot.parent.transform.eulerAngles.y + maxTurnAngle), headRoot.transform.eulerAngles.z);
				headRoot.localPosition = desiredHeadPosition;
			}
		}

		/// <summary>
		/// Gets a position in a direction in a cone shape.
		/// </summary>
		/// <param name="startPosition">Origin of cone shape</param>
		/// <param name="direction">Direction the cone should travel</param>
		/// <param name="length">How far the cone should go</param>
		/// <param name="radius">Radius of the cone end</param>
		/// <returns></returns>
		private Vector3 GetPositionInCome(Vector3 startPosition, Vector3 direction, float length, float radius)
		{
			Vector3 randomCircle = Random.insideUnitCircle * radius;
			Vector3 finalPosition = (startPosition + (direction * length)) + (direction + new Vector3(randomCircle.x, randomCircle.y));

			return finalPosition;
		}

	}

}
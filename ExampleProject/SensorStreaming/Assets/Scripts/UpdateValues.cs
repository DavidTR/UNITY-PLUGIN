using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class UpdateValues: MonoBehaviour {
	private Sphero m_Sphero;
	
	private bool streaming = false;
	private SpheroAccelerometerData.Acceleration acceleration = 
		new SpheroAccelerometerData.Acceleration();

	private float pitch = 0.0f;
	private float roll = 0.0f;
	private float yaw = 0.0f;
	private float q0 = 1.0f;
	private float q1 = 1.0f;
	private float q2 = 1.0f;
	private float q3 = 1.0f;
	
		/* Use this for initialization */
	void ViewSetup() {
		// Get Connected Sphero
		SpheroDeviceMessenger.SharedInstance.NotificationReceived +=
			 ReceiveNotificationMessage;
		if( SpheroProvider.GetSharedProvider().GetConnectedSpheros().Count == 0 )
			Application.LoadLevel("SpheroConnectionScene");
	}

	// Use this for initialization
	void Start () {
		ViewSetup();
	}
	
	// Update is called once per frame
	void Update () {
		if (!streaming && SpheroProvider.GetSharedProvider().GetConnectedSpheros().Count  > 0) {
		SpheroDeviceMessenger.SharedInstance.AsyncDataReceived += ReceiveAsyncMessage;	
		List<Sphero> spheroList = SpheroProvider.GetSharedProvider().GetConnectedSpheros();
		m_Sphero = spheroList[0];
		m_Sphero.EnableControllerStreaming(20, 1,
			SpheroDataStreamingMask.AccelerometerFilteredAll |
			SpheroDataStreamingMask.QuaternionAll |
			SpheroDataStreamingMask.IMUAnglesFilteredAll);

		streaming = true;
		}	
	}
	
	void OnApplicationPause(bool pause) {
		if (pause) {
			if (streaming) {
				SpheroDeviceMessenger.SharedInstance.AsyncDataReceived -= 
					ReceiveAsyncMessage;	
				m_Sphero.DisableControllerStreaming();
				streaming = false;
			} 
			SpheroDeviceMessenger.SharedInstance.NotificationReceived -= ReceiveNotificationMessage;
			SpheroProvider.GetSharedProvider().DisconnectSpheros();
		}else {
			ViewSetup();
			streaming = false;
		}
	}
		
	public GUIStyle boxStyle;
	public GUIStyle labelStyle;
	
	void OnGUI() {
		
		GUI.BeginGroup(new Rect(10, 10, 200, 400), boxStyle);
		GUI.Box(new Rect(0,0,200, 400), "Stream");
		GUI.Label(new Rect(4,30,180,36), "accel x: " + acceleration.X, labelStyle);
		GUI.Label(new Rect(4,66,180,36), "accel y: " + acceleration.Y, labelStyle);
		GUI.Label(new Rect(4,102,180,36), "accel z: " + acceleration.Z, labelStyle);
		GUI.Label(new Rect(4,138,180,36), "pitch: " + pitch, labelStyle); 
		GUI.Label(new Rect(4,174,180,36), "roll: " + roll, labelStyle);
		GUI.Label(new Rect(4,210,180,36), "yaw: " + yaw, labelStyle);
		GUI.Label(new Rect(4,246,180,36), "q0: " + q0, labelStyle);
		GUI.Label(new Rect(4,282,180,36), "q1: " + q1, labelStyle);
		GUI.Label(new Rect(4,318,180,36), "q2: " + q2, labelStyle);
		GUI.Label(new Rect(4,354,180,36), "q3: " + q3, labelStyle);
		GUI.EndGroup();
	}
	
	private void ReceiveAsyncMessage(object sender, 
			SpheroDeviceMessenger.MessengerEventArgs eventArgs)
	{
		SpheroDeviceSensorsAsyncData message = 
			(SpheroDeviceSensorsAsyncData)eventArgs.Message;
		SpheroDeviceSensorsData sensorsData = message.Frames[0];
		
		acceleration = sensorsData.AccelerometerData.Normalized;

		pitch = sensorsData.AttitudeData.Pitch;
		roll = sensorsData.AttitudeData.Roll;
		yaw = sensorsData.AttitudeData.Yaw;

		q0 = sensorsData.QuaternionData.Q0;
		q1 = sensorsData.QuaternionData.Q1;
		q2 = sensorsData.QuaternionData.Q2;
		q3 = sensorsData.QuaternionData.Q3; 
	}

	/*
	 * Callback to receive connection notifications 
	 */
	private void ReceiveNotificationMessage(object sender, SpheroDeviceMessenger.MessengerEventArgs eventArgs)
	{
		SpheroDeviceNotification message = (SpheroDeviceNotification)eventArgs.Message;
		if( message.NotificationType == SpheroDeviceNotification.SpheroNotificationType.DISCONNECTED ) {
			m_Sphero.ConnectionState = Sphero.Connection_State.Disconnected;
			Application.LoadLevel("NoSpheroConnectedScene");
		}
	}
	
}

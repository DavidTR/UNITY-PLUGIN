using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Collections.Generic;

#if UNITY_ANDROID

public class SpheroProviderAndroid : SpheroProvider {
	
	// Link into the JNI
	private AndroidJavaObject m_RobotProvider;
	
	/*
	 * Get the Robot Provider for Android 
	 */
	public SpheroProviderAndroid() : base() {
		
		// The SDK uses alot of handlers that need a valid Looper in the thread, so set that up here
        using (AndroidJavaClass jc = new AndroidJavaClass("android.os.Looper"))
        {
        	jc.CallStatic("prepare");
        }
		
		using (AndroidJavaClass jc = new AndroidJavaClass("orbotix.robot.base.RobotProvider"))
	    {
			m_RobotProvider = jc.CallStatic<AndroidJavaObject>("getDefaultProvider");
		}
		// Sign up for notifications on Sphero connections
		SpheroDeviceMessenger.SharedInstance.NotificationReceived += ReceiveNotificationMessage;
		FindRobots();
	}
	
	/*
	 * Callback to receive connection notifications 
	 */
	private void ReceiveNotificationMessage(object sender, SpheroDeviceMessenger.MessengerEventArgs eventArgs)
	{
		SpheroDeviceNotification message = (SpheroDeviceNotification)eventArgs.Message;
		Sphero notifiedSphero = GetSphero(message.RobotID);
		if( message.NotificationType == SpheroDeviceNotification.SpheroNotificationType.CONNECTED ) {
			notifiedSphero.ConnectionState = Sphero.Connection_State.Connected;
		}
		else if( message.NotificationType == SpheroDeviceNotification.SpheroNotificationType.DISCONNECTED ) {
			notifiedSphero.ConnectionState = Sphero.Connection_State.Disconnected;
		}
		else if( message.NotificationType == SpheroDeviceNotification.SpheroNotificationType.CONNECTION_FAILED ) {
			notifiedSphero.ConnectionState = Sphero.Connection_State.Failed;
		}
	}
	
	/*
	 * Call to properly disconnect Spheros.  Call in OnApplicationPause 
	 */
	override public void DisconnectSpheros() {
		m_RobotProvider.Call("disconnectControlledRobots");	
	}
	
	/* Need to call this to get the robot objects that are paired from Android */
	override public void FindRobots() {
		// Only run this stuff if the adapter is enabled
		if( IsAdapterEnabled() ) {
			m_RobotProvider.Call("findRobots");  
			AndroidJavaObject pairedRobots = m_RobotProvider.Call<AndroidJavaObject>("getRobots");
			int pairedRobotCount = pairedRobots.Call<int>("size");
			// Initialize Sphero array
			m_PairedSpheros = new Sphero[pairedRobotCount];
			// Create Sphero objects for the Paired Spheros
			for( int i = 0; i < pairedRobotCount; i++ ) {
				// Set up the Sphero objects
				AndroidJavaObject robot = pairedRobots.Call<AndroidJavaObject>("get",i);
				string bt_name = robot.Call<string>("getName");
				string bt_address = robot.Call<string>("getUniqueId");
				m_PairedSpheros[i] = new SpheroAndroid(robot, bt_name, bt_address);
			}
		}	
	}
	
	/* Check if bluetooth is on */
	override public bool IsAdapterEnabled() {
		return m_RobotProvider.Call<bool>("isAdapterEnabled"); 		
	}
	
	/* Connect to a robot at index */
	override public void Connect(int index) {
		// Don't try to connect to multiple Spheros at once
		if( GetConnectingSphero() != null ) return;
		
		m_RobotProvider.Call("control", ((SpheroAndroid)m_PairedSpheros[index]).AndroidJavaSphero);
		m_RobotProvider.Call<AndroidJavaObject>("connectControlledRobots");
		m_PairedSpheros[index].ConnectionState = Sphero.Connection_State.Connecting;
	}	
}

#endif
	

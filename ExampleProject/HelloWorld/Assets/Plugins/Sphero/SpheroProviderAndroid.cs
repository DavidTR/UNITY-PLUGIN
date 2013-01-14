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
	}
	
	/*
	 * Call to properly disconnect Spheros.  Call in OnApplicationPause 
	 */
	override public void DisconnectSpheros() {
		m_RobotProvider.Call("disconnectControlledRobots");	
	}
	
	/* Need to call this to get the robot objects that are paired from Android */
	override public bool FindRobots() {
		// Only run this stuff if the adapter is enabled
		if( IsAdapterEnabled() ) {
			m_RobotProvider.Call("findRobots");  
			AndroidJavaObject pairedRobots = m_RobotProvider.Call<AndroidJavaObject>("getRobots");
			int pairedRobotCount = pairedRobots.Call<int>("size");
			// Initialize Sphero array
			base.m_PairedSpheros = new Sphero[pairedRobotCount];
			// Create Sphero objects for the Paired Spheros
			for( int i = 0; i < pairedRobotCount; i++ ) {
				// Set up the Sphero objects
				AndroidJavaObject robot = pairedRobots.Call<AndroidJavaObject>("get",i);
				string bt_name = robot.Call<string>("getName");
				string bt_address = robot.Call<string>("getUniqueId");
				m_PairedSpheros[i] = new Sphero(robot, bt_name, bt_address);
			}
			return true;
		}	
		return false;
	}
	
	/* Check if bluetooth is on */
	override public bool IsAdapterEnabled() {
		return m_RobotProvider.Call<bool>("isAdapterEnabled"); 		
	}
	
	/* Connect to a robot at index */
	override public void Connect(int index) {
		// Don't try to connect to multiple Spheros at once
		if( GetConnectingSphero() != null ) return;
		
		m_RobotProvider.Call("control", m_PairedSpheros[index].AndroidJavaSphero);
		m_RobotProvider.Call<AndroidJavaObject>("connectControlledRobots");
		m_PairedSpheros[index].ConnectionState = Sphero.Connection_State.Connecting;
	}	
}

#endif
	

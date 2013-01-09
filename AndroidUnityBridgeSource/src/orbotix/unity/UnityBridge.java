package orbotix.unity;

import java.util.HashMap;

import orbotix.robot.base.BackLEDOutputCommand;
import orbotix.robot.base.DeviceAsyncData;
import orbotix.robot.base.DeviceMessenger;
import orbotix.robot.base.DeviceSensorsAsyncData;
import orbotix.robot.base.Robot;
import orbotix.robot.base.SetDataStreamingCommand;
import orbotix.robot.base.StabilizationCommand;

/**
 * This is the Native Android code that manages sending data streaming to Unity using a function pointer in C++ libunity_bridge.so
 */

public class UnityBridge {

    /**
     * Singleton UnityBridge instance
     */
    private static UnityBridge sharedBridge = new UnityBridge();
    
    /**
     * Holds the profile for updating data streaming for multiple robots
     */
    HashMap<Robot, RobotDataStreamingProfile> mRobotDataStreamingProfiles = new HashMap<Robot, RobotDataStreamingProfile>();
	
	static {
		System.loadLibrary("unity_bridge");
	}
	
	/**
	 * Default Constructor
	 */
	public UnityBridge() {}
	
	/**
	 * Accessor for the shared unity bridge
	 * @return Unity Bridge
	 */
	public static UnityBridge sharedBridge() {
		return sharedBridge;
	}
	
	/**
	 * Start Streaming Data to Unity 
	 * @param robot the Robot to stream data from
     * @param divisor Divisor of the maximum sensor sampling rate (400 Hz)
     * @param packetFrames Number of samples created on the device before it sends a packet to the phone with samples
     * @param sensorMask Bitwise selector of data sources to stream
     * @param packetCount Packet count (set to 0 for unlimited streaming) 
	 */
	public void setDataStreaming(Robot robot, int divisor, int packetFrames, long sensorMask, int packetCount) {
		// Remove old profile
		if( mRobotDataStreamingProfiles.containsKey(robot) ) {
			mRobotDataStreamingProfiles.remove(robot);
		}
		// Add the robot to the profile
		RobotDataStreamingProfile robotDataStreamingProfile = new RobotDataStreamingProfile(divisor, packetFrames, sensorMask, packetCount);
		mRobotDataStreamingProfiles.put(robot, robotDataStreamingProfile);
		SetDataStreamingCommand.sendCommand(robot, divisor, packetFrames, sensorMask, robotDataStreamingProfile.getPacketCount());
	}
	
	/**
	 * Enable controller data streaming with infinite packets
	 * @param robot the Robot to stream data from
     * @param divisor Divisor of the maximum sensor sampling rate (400 Hz)
     * @param packetFrames Number of samples created on the device before it sends a packet to the phone with samples
     * @param sensorMask Bitwise selector of data sources to stream
	 */
	public void enableControllerStreamingWithSampleRateDivisor(Robot robot, int divisor, int packetFrames, long sensorMask) {
		// Turn off stabilization and turn on back LED
		StabilizationCommand.sendCommand(robot, false);
		BackLEDOutputCommand.sendCommand(robot, 1.0f);
		// Request data streaming
		this.setDataStreaming(robot, divisor, packetFrames, sensorMask, 0);
	}
	
	/**
	 * Disable controller data streaming for a Robot
	 * @param robot the Robot to stream data from
	 */
	public void disableControllerStreaming(Robot robot) {
		// Turn on stabilization and turn off back LED
		StabilizationCommand.sendCommand(robot, false);
		BackLEDOutputCommand.sendCommand(robot, 1.0f);
		// Disable data streaming and delete profile
		SetDataStreamingCommand.sendCommand(robot,0,0,0,0);
		if( mRobotDataStreamingProfiles.containsKey(robot) ) {
			mRobotDataStreamingProfiles.remove(robot);
		}
	}
	
	/**
     * AsyncDataListener that will be assigned to the DeviceMessager, listen for streaming data, and then do the
     */
    private DeviceMessenger.AsyncDataListener mDataListener = new DeviceMessenger.AsyncDataListener() {
        @Override
        public void onDataReceived(DeviceAsyncData data) {

        	Robot robot = data.getRobot();
        	
            if(data instanceof DeviceSensorsAsyncData){

            	// Check if the robot has a profile first
            	if( mRobotDataStreamingProfiles.containsKey(robot) ) {
	            	RobotDataStreamingProfile dataStreamingProfile = mRobotDataStreamingProfiles.get(robot);
	                // If we are getting close to packet limit, request more
	                if( dataStreamingProfile.incramentPacketCounter() ) {
	                	// Only request more if the packet counter is infinite
	                    if( dataStreamingProfile.isInfinite() ) {
	                    	SetDataStreamingCommand.sendCommand(robot, 
	                    			dataStreamingProfile.getDivisor(), 
	                    			dataStreamingProfile.getPacketFrames(), 
	                    			dataStreamingProfile.getSensorMask(), 
	                    			dataStreamingProfile.getPacketCount());
	                    	
	                    	dataStreamingProfile.resetPacketCounter();
	                    }
	                }
	                UnityBridge.this.handleDataStreaming(""+dataStreamingProfile.getPacketCounter());
            	}
            	
            	// Call Encoder!
                //get the frames in the response
//                List<DeviceSensorsData> data_list = ((DeviceSensorsAsyncData)data).getAsyncData();
//                if(data_list != null){
//                    //Iterate over each frame
//                    for(DeviceSensorsData datum : data_list){
//
//                    }
//                }
            }
        }
    };
    
    /**
     * Private inner class to keep track of each data streaming profile for multiple robots
     */
    private class RobotDataStreamingProfile {
    	
        /**
         * Data Streaming Packet Counts
         */
        private final static int TOTAL_PACKET_COUNT = 200;
        private final static int PACKET_COUNT_THRESHOLD = 50;
    	
        private int mPacketCounter;
        private int mDivisor;
        private int mPacketFrames;
        private long mSensorMask;
        private int mPacketCount;
    	
    	public RobotDataStreamingProfile(int divisor, int packetFrames, long sensorMask, int packetCount) {
			mDivisor = divisor;
			mPacketFrames = packetFrames;
			mSensorMask = sensorMask;
			mPacketCount = packetCount;
		}
    	
    	/**
    	 * Incraments the packet counter
    	 * @return true if limit has been reached, false otherwise
    	 */
    	public boolean incramentPacketCounter() {
    		if( ++mPacketCounter > (TOTAL_PACKET_COUNT-PACKET_COUNT_THRESHOLD) ) {
    			return true;
    		}
    		return false;
    	}
    	
    	/**
    	 * Reset the packet counter after a new data streaming command is sent
    	 */
    	public void resetPacketCounter() {
    		mPacketCounter = 0;
    	}
    	
    	/**
    	 * Check if code should continue requesting more data
    	 * @return true if robot wants more data, false otherwise
    	 */
    	public boolean isInfinite() {
    		return mPacketCount == 0;
    	}
    	
    	/**
    	 * Getters
    	 */
    	public int getPacketCounter() { return mPacketCounter; }
    	public int getDivisor() { return mDivisor; }
    	public int getPacketFrames() { return mPacketFrames; }
    	public long getSensorMask() { return mSensorMask; }
    	
    	/**
    	 * Get the Packet Count for this Robot
    	 * @return the packetcounter or 200 for infinite streaming
    	 */
    	public int getPacketCount() { 
    		return (mPacketCount==0)?TOTAL_PACKET_COUNT:mPacketCount; 
    	}
    }
    
    ///////////////////////////
    /// NDK Native Methods
    ///////////////////////////
    
    private native void handleDataStreaming(String data);
}

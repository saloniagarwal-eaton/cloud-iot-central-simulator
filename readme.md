IoT_Central_Simulator
This simulator is used for simulating IoT Central devices. It will provision the device and using device client send the data to IoT Central.

Input
The file with the device tree structure must be provided. This parameter should just be the file name.
The simulator will look in the .\bin\Debug\netcoreapp3.1 folder to locate the file.

---------------------------------------------------------------------------------------------
In the App.config file add the necessary values for:
ProvisioningGlobalDeviceEndpoint - Global endpoint of IoT hub device provisioning service to perform the device provisioning related operation.
ProvisioningIdScope - ID assigned to each device provisioning service when created. Used to uniquely identify the DPS. 
UseGroupSymmetricKey - Set to true or false depending on if devices should be provisioned using group symmetric keys or individual device key. 
                        - For true, add the necessary value for GroupSymmetricKey (Group-level Primary/Secondary SAS key to generate keys for your individual device(s)).
                        - For false, add the necessary value for DevicePrimaryKey (Primary/Secondary SAS key used by device to connect to IoT Central application).
SendTelemetryData - Set to true or false depending on if simulator should send Telemetry data or Alarm data.

--------------------------------------------------------------------------------------------
Steps to start IoT Central Simulator:
•	Navigate to .\bin\Debug\netcoreapp3.1 location.
•	Import the json file like “devicetree.json” in folder .\bin\Debug\netcoreapp3.1. 
•	Import all the required DTDL json files(as mentioned in devicetree.json) like “devicetemplate.json” in folder .\bin\Debug\netcoreapp3.1\DTDLFiles.
•	Start the simulator by launching .exe
•	Wait till all the devices are successfully connected to the IoT Central and sending data....

--------------------------------------------------------------------------------------------
Json Files:

- The devicetree.json file must be of structure:
    {
      "gatewayId": "",
      "dtdlFileName": "",
      "childDevices": [
	    {
	      "deviceId": "",
	      "dtdlFileName": ""
	    }
      ]
    }

•	Populate the gatewayId, dtdl filename for gateway device. (eg- "dtdlFileName": "RoomAtmosphereDTDL.json")
•	Add the element(deviceId, dtdl filename) in the childDevices array for all downstream devices, if no downstream devices are there leave childDevices array empty.
Note: This simulator supports multiple templates. Gateway and Downstream devices may use different or same template. Just provide the dtdlFileName according to that.

- The devicetemplate.json(i.e. DTDL) file must be exported from IoT Central application. Steps to export:
•	On the left pane in IoT Central, select Device Template.
•	Open the desired device template.
•	Select {}EditDTDL from the right side.
•	Copy the content of that DTDL and paste it in new Json file.
•	Save the Json file with a proper name and populate the field "dtdlFileName" with this filename in devicetree.json file.

---------------------------------------------------------------------------------------------

😊!!!Happy Simulation!!!!😊
﻿using HA4IoT.Actuators;
using HA4IoT.Automations;
using HA4IoT.Contracts.Areas;
using HA4IoT.Contracts.Services.System;
using HA4IoT.Hardware.CCTools;
using HA4IoT.Hardware.I2CHardwareBridge;
using HA4IoT.Sensors;
using HA4IoT.Services.Areas;
using HA4IoT.Services.Devices;
using System.Diagnostics;
using HA4IoT.Controller.Dnf.Enums;
using HA4IoT.Sensors.MotionDetectors;
using HA4IoT.Actuators.Lamps;
using HA4IoT.Extensions.Extensions;

namespace HA4IoT.Controller.Dnf.Rooms
{

    internal partial class ToiletConfiguration 
    {
        private readonly IDeviceService _deviceService;
        private readonly IAreaService _areaService;
        private readonly SensorFactory _sensorFactory;
        private readonly ActuatorFactory _actuatorFactory;
        private readonly AutomationFactory _automationFactory;

        public ToiletConfiguration(IDeviceService deviceService,
                                    IAreaService areaService,
                                    SensorFactory sensorFactory,
                                    ActuatorFactory actuatorFactory,
                                    AutomationFactory automationFactory)  
        {
            _deviceService = deviceService;
            _areaService = areaService;
            _sensorFactory = sensorFactory;
            _actuatorFactory = actuatorFactory;
            _automationFactory = automationFactory;
        }

        public void Apply()
        {
            var input = _deviceService.GetDevice<HSPE16InputOnly>(CCToolsDevices.HSPE16_16);
            var relays = _deviceService.GetDevice<HSREL8>(CCToolsDevices.HSRel8_32);
            var tempSensor = _deviceService.GetTempSensor((int)ToiletElements.TempSensor);
            var humiditySensor = _deviceService.GetHumiditySensor((int)ToiletElements.TempSensor);

            var room = _areaService.CreateArea(Room.Toilet);

            _sensorFactory.RegisterTemperatureSensor(room, ToiletElements.TempSensor, tempSensor);
            _sensorFactory.RegisterHumiditySensor(room, ToiletElements.HumiditySensor, humiditySensor);
            _sensorFactory.RegisterMotionDetector(room, ToiletElements.MotionDetector, input[HSPE16Pin.GPIO3]);

            _actuatorFactory.RegisterMonostableLamp(room, ToiletElements.Light, relays[HSREL8Pin.Relay2], input[HSPE16Pin.GPIO15]);

            _automationFactory.RegisterTurnOnAndOffAutomation(room, ToiletElements.LightAutomation)
             .WithTrigger(room.GetMotionDetector(ToiletElements.MotionDetector))
             .WithTarget(room.GetMonostableLamp(ToiletElements.Light));      
        }
    }
}
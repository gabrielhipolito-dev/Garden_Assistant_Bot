# Setup Guide

## Prerequisites

- Windows 10/11 machine
- Visual Studio 2022 + .NET 8 SDK
- Arduino IDE
- USB cable for Arduino Nano

## Hardware bring-up

1. Assemble the drivetrain, servo arm, and sensors as described in [hardware](hardware.md).
2. Ensure power wiring and motor polarity are correct.
3. Pair the HC-05 Bluetooth module with your PC and note the COM port.

## Firmware upload

1. Open `firmware/arduino/lupa_tusok_updated_ver.ino` in Arduino IDE.
2. Select **Board → Arduino Nano** and **Processor → ATmega328P (Old Bootloader)**.
3. Choose the correct COM port and click **Upload**.

## Desktop app

1. Open `rc_controller.sln` in Visual Studio 2022.
2. Build and run the `rc_controller` project.
3. In the app, select the Bluetooth COM port and click **Connect**.

## Validation checklist

- UI connects to the Bluetooth COM port.
- Movement controls respond as expected.
- Battery, moisture, humidity, and temperature readings update.

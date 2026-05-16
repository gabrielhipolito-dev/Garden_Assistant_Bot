# Architecture

## Overview

Garden Assistant Bot is composed of three primary subsystems:

1. **Desktop controller (WPF)**: operator UI for driving, servo control, and sensor readouts.
2. **Arduino firmware**: real-time control of motors, servos, and sensors.
3. **Hardware platform**: drivetrain, servo arm, and environmental sensors.

## Data flow

1. The WPF app opens a Bluetooth serial connection (HC-05 @ 9600 baud).
2. The operator sends single-character commands (movement, servo control, sensor query).
3. The Arduino firmware performs the action and streams status/sensor values back.
4. The desktop app parses and displays responses in the UI.

## Responsibilities

- **WPF app**: UX, input mapping, connection lifecycle, and telemetry rendering.
- **Arduino firmware**: actuator control, sensor sampling, and serial responses.
- **Hardware**: power, motion, and sensing integration.

## Deployment boundaries

- Desktop app runs on Windows with .NET 8.
- Firmware runs on Arduino Nano with attached sensors and drivers.
- Communication is a simple serial protocol to keep latency low.

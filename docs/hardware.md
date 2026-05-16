# Hardware BOM

## Core electronics

- Arduino Nano (CH340)
- HC-05 Bluetooth module
- L298N motor driver
- DHT22 temperature/humidity sensor
- HW-080 soil moisture sensor
- Voltage sensor module

## Actuators and power

- 2x DC drive motors
- 3x MG996R servo motors
- 2x 18650 batteries + holder
- Toggle switch

## Mechanical

- 2WD chassis
- Servo arm mount
- Fasteners, brackets, and wiring

## Wiring notes

- Ensure common ground across Arduino, motor driver, and sensor power.
- Keep sensor lines away from motor leads to reduce noise.
- Verify polarity before powering the motor driver.

CAD, wiring diagrams, and enclosure files will live under `hardware/`.

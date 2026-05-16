# Firmware Notes

## Location

`firmware/arduino/lupa_tusok_updated_ver.ino`

## Serial configuration

- Baud rate: **9600**
- Data: **8N1**

## Sensors

- DHT22 for temperature and humidity
- HW-080 soil moisture sensor (powered by GPIO)
- Battery voltage through analog divider

## Actuators

- 2WD DC motors through L298N driver
- 3x MG996R servos for the arm

## Key behaviors

- Movement is driven by `w/a/s/d` commands.
- Sensor queries return newline-delimited numeric values.
- Servo controls run continuously until stopped with `p`.

For exact command mapping, see [communication-protocol.md](communication-protocol.md).

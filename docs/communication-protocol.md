# Communication Protocol

## Transport

- Bluetooth serial (HC-05)
- 9600 baud, 8N1

## Commands (single-character)

### Movement

| Command | Action |
| --- | --- |
| `w` | Move forward |
| `a` | Turn left |
| `s` | Move backward |
| `d` | Turn right |
| `x` | Stop motors |
| `z` | Decrease speed step |
| `c` | Increase speed step |

Speed steps are 0-3. The firmware replies with `Motor Speed: <step>/3` when adjusted.

### Sensors

| Command | Action | Response |
| --- | --- | --- |
| `v` | Battery voltage | Numeric voltage value |
| `b` | Soil moisture | Percent (0-100) |
| `n` | Humidity | Percent |
| `m` | Temperature | Celsius |

### Servo arm

| Command | Action |
| --- | --- |
| `t` | Servo A forward |
| `g` | Servo A backward |
| `y` | Servo B up |
| `h` | Servo B down |
| `u` | Servo R counterclockwise |
| `i` | Servo R clockwise |
| `p` | Stop all servos |

## Responses

- Sensor reads return numeric values followed by a newline.
- Movement/servo commands return short status strings for logging.

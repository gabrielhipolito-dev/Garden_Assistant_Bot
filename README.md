# Garden Assistant Bot (GAB)

This project includes a robot which uses a variety of sensors & motors to measure and provide data from your garden plants, as well as a user interface for communicating and controlling the robot.
 
---

##  Visual Overview of GAB Project

### WPF User Interface (UI)
<br>

<p align="center">
  <img src="images/GUI_GAB.png" alt="WPF User Interface for Robot Control & Plant Monitoring" width="500">
  <br>
  <em>Fig. 1 — WPF User Interface for Robot Control & Plant Monitoring</em><br>
  The UI provides control, Bluetooth connection status, and sensor data for soil moisture, temperature, and humidity.
</p>
<br><br>

### GAB  Overview
<br>

<p align="center">
    <img src="images/GAB.png" alt="View of Garden Assistant Bot (GAB)" width="500">
  <br>
  <em>Fig. 2 — VIEW Garden Assistant Bot (GAB) Hardware</em><br>
  This figure displays the GAB, showing the sensor placements and main control board.
</p>
<br>

---
### Developed By
- Jeff Bayhon  
- Lance Dador  
- Giuliano de Guzman  
- Gabriel Hipolito  

---

## Languages Used
- **Arduino** (Nano microcontroller firmware)
- **C#** (Windows .NET Application)

---

## Features
- 2-Wheel Drive movement system  
- 4-DOF robotic servo arm  
- Bluetooth communication (Serial)  
- Live sensor readings:
  - Battery voltage  
  - Soil moisture  
  - Air humidity  
  - Air temperature  
- GUI desktop controller
- Keyboard controls for easy operation

---

## Requirements

### Software
- **Visual Studio 2022**
- **.NET 8**
- **Arduino IDE**

### Hardware
- Arduino Nano CH340 
- HC-05 Bluetooth Module 
- L298N Motor Driver 
- 3x MG996R Servo Motor 
- Voltage Sensor Module 
- DHT22 Sensor 
- L298N H-Bridge Driver
- HW-080 soil moisture sensor 
- 2x 18650 Battery Cell
- 2 cell 18650 battery holder
- Toggle Switch 
- Jumper Wires 


---

##  Folder Structure

```
/rc_controller
  └── /ARDUINO_NANO
      └── lupa_tusok_updated_ver.ino
  └── /img
  └── app.xaml
  └── app.xaml.cs
  └── AssemblyInfo.cs
  └── MainWindow.xaml
  └── MainWindow.xaml.cs
  └── rc_controller.csproj
```

## Steps to Connect and the Program

1. Clone the repository:

```bash
git clone https://github.com/YourRepoHere/rc_controller.git
cd rc_controller
```

2. Upload Arduino:

   * Open `/ARDUINO_NANO/lupa_tusok_updated_ver.ino` in Arduino IDE
   * Connect Arduino Nano via USB
   * Select board: `Tools → Board → Arduino Nano`
   * Select processor: `ATmega328P (Old Bootloader)` (for CH340)
   * Select COM port (example: COM5)
   * Click **Upload**
3. Run the Application(Visual Studio):

   * Open `rc_controller.csproj` in Visual Studio
   * Open `MainWindow.xaml`
   * Click **Start / Run** to launch the app
4. Connect to the RC Car:

   * Select Bluetooth COM port in the app
   * Click **Connect**
   * Use the GUI to drive the robot and monitor sensor data

## Notes

* Ensure the robot is powered **before** connecting the app
* Use correct COM port for Bluetooth (not USB after upload)
* Place soil sensor in the plant soil for accurate readings

## How to Run 

1. Pair your PC with the robot via **Bluetooth**
2. Check the COM port assigned to the Bluetooth module (Device Manager)
3. Open the desktop app
4. Select the COM port and click connect
5. Control the robot using the on-screen buttons or keyboard shortcuts below:

### Keyboard Controls

| Function | Keys |
|--------|------|
Forward / Back / Left / Right (For Motor Movement) | `W` `S` `A` `D`  
Decrease Motor Speed / Increase Motor Speed | `Z` / `C`  
Battery Reading | `V`  
Soil Moisture Reading | `B`  
Humidity Reading | `N`  
Temperature Reading | `M`  

### Servo Arm Controls

| Servo | Keys |
|-------|-----|
Servo A (X - AXIS) | `T` / `G`  
Servo B (Y - AXIS) | `Y` / `H`  
Servo R (Z - ROTATION) | `U` / `I`  

---




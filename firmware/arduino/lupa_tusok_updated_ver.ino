// C++ code
// Dador_PC = COM8 USB, COM10 BT
// Gab laptop = COM5 USB, COM4 BT
#include <Servo.h>
#include <DHT.h>
#define DHTTYPE DHT22

// motor config
const int enA = 5, enB = 6, in4 = 7, in3 = 8, in2 = 9, in1 = 10;
int msI = 2;
int motorSpdL[] = {0, 130, 185, 252};
int motorSpdR[] = {0, 153, 210, 253}; // adjusted to ensure approximately same voltage level at each motor

// sensor config
const int battPin = A0, soilPin = A3, soilVCC = A4, dhtPin = A2;
float temp = 0.0, hum = 0.0;
DHT dht(dhtPin, DHTTYPE);

// servo config
const int servoA = 4, servoB = 3, servoR = 2, l_limit = 55, u_limit = 195;
int sA = 140, sB =35,sR = 90, gap = 4;
bool sA_up = false, sA_down = false, sB_up = false, sB_down = false, sR_up = false, sR_down = false;
Servo myServoA, myServoB, myServoR;



void setup()
{
  Serial.begin(9600);
  dht.begin();
  
  int out[] = {soilVCC, in1, in2, in3, in4, enA, enB};
  for (int i=0; i<7; i++){
    pinMode(out[i], OUTPUT);
  }
  
  int inp[] = {battPin, soilPin};
  for (int i=0; i<2; i++){
    pinMode(inp[i], INPUT);
  }
  
  myServoA.write(sA);
  myServoA.attach(servoA);
  myServoB.write(sB);
  myServoB.attach(servoB);
  myServoR.attach(servoR);
}

void loop()
{
  if (Serial.available() > 0) {
    // read the incoming serial command:
    char serialData = Serial.read();
    
    switch (serialData){
      // v, battery, b=moisture, n=humidity, m=temperature
      case 'v':{       
      	checkBatt();
      	break;
        }
      case 'b':{
      	checkMoisture();
      	break;
        }
      case 'n':{
      	checkHum();
      	break;
        }
      case 'm':{
      	checkTemp();
      	break;
        }
      // wasd for direction control
      case 'w':{
        int a[] = {0, 1, 0, 1}, b[] = {1, 1};
      	move_motor(a, b, "Direction: Forward");
      	break;
        }
      case 'a':{
        int a[] = {0, 1, 1, 0}, b[] = {1, 0};
      	move_motor(a, b, "Direction: Left");
      	break;
        }
      case 's':{
        int a[] = {1, 0, 1, 0}, b[] = {1, 1};
      	move_motor(a, b, "Direction: Backward");
      	break;
      }
      case 'd':{
        int a[] = {1, 0, 0, 1}, b[] = {0, 1};
      	move_motor(a, b, "Direction: Right");
      	break;
      }
      // z=lower speed, x=stop, c=increase speed
      case 'x':{
      	stop();
      	break;
      }
      case 'z':{
        if (0<msI && msI<=3){
          msI--;
          Serial.println("Motor Speed: " + String(msI)+ "/3");
        }
      	break;
      }
      case 'c':{
      	if (0<=msI && msI<3){
          msI++;
          Serial.println("Motor Speed: " + String(msI)+ "/3");
        }
      	break;
      }
      // t/g = servo A, y/h = servo B, u/i = servo R
      case 't': {
        sA_down = true, sA_up = false;
        Serial.println("Servo arm going forward");
        break;
      }
      case 'g': {
        sA_up = true, sA_down = false;
        Serial.println("Servo arm going backward");
        break;
      }
      case 'y': {
        sB_down = true, sB_up = false;
        Serial.println("Servo arm going up");
        break;
      }
      case 'h': {
        sB_up = true, sB_down = false;
        Serial.println("Servo arm going down");
        break;
      }
      case 'u': {
        sR_up = true, sR_down = false;
        Serial.println("Servo arm turning counterclockwise");
        break;
      }
      case 'i': {
        sR_down = true, sR_up = false;
        Serial.println("Servo arm turning clockwise");
        break;
      }
      case 'p':{
        sA_up = sA_down = sB_up = sB_down = sR_up = sR_down = false;
        Serial.println("All servos stopped");
        break;
      }
    }
  }

  // perform servo movement
  if (sA_down) {
    if ((sA - gap) + sB <= u_limit && (sA - gap) + sB >= l_limit) // check relative angle between servo A & B: (30 < angle < 150) to prevent arm damage
      moveServo(0, 180, 0, sA, myServoA);
  }
  else if (sA_up) {
    if ((sA + gap) + sB <= u_limit && (sA + gap) + sB >= l_limit)
      moveServo(0, 180, 1, sA, myServoA);
  }

  if (sB_down) {
    if (sA + (sB - gap) <= u_limit && sA + (sB - gap) >= l_limit)
      moveServo(0, 125, 0, sB, myServoB);
  }
  else if (sB_up) {
    if (sA + (sB + gap) <= u_limit && sA + (sB + gap) >= l_limit)
      moveServo(0, 125, 1, sB, myServoB);
  }
  
  if (sR_down){
    moveServo(0,180, 0, sR, myServoR);
  }
  else if (sR_up){
    moveServo(0,180, 1, sR, myServoR);
  }

  delay(100);
}

// ---------------------------------------------- // 
void checkBatt(){
  float fivevoltpinval = 5.0;
  float batt = (analogRead(battPin)*5*fivevoltpinval/1024.00)+0.04;
  Serial.println(String(batt));
  return;
}

void checkMoisture(){
  // wet = 386, dry = 1007;

  digitalWrite(soilVCC, HIGH);
  delay(50);
  float moist = analogRead(soilPin);
  float moistval = 100.00 - 100*(moist-386)/(1007-386);
  Serial.println(String(constrain(moistval, 0, 100)));
  delay(250);
  digitalWrite(soilVCC, LOW);
  return;
}

void checkTemp(){
  temp = dht.readTemperature();
  Serial.println(String(temp));
  return;
}

void checkHum(){
  hum = dht.readHumidity();
  Serial.println(String(hum));
  return;
}

// ---------------------------------------------- // 

void move_motor(int dirVal[], int mVal[] , String msg){
  int motorpins[] = {in1, in2, in3, in4};

  for (int i=0; i<4; i++){
    digitalWrite(motorpins[i], dirVal[i]);
  }
  analogWrite(enA, mVal[0]*motorSpdR[msI]);
  analogWrite(enB, mVal[1]*motorSpdL[msI]);
  Serial.println(msg);
}

void stop(){
  analogWrite(enA, 0);
  analogWrite(enB, 0);
  Serial.println("Stopped Motors");
  return;
}

// ---------------------------------------------- // 

void moveServo(int l_limit, int u_limit, int val, int &sVal, Servo &sName){
  if (val && sVal<u_limit){
    sVal+=gap;
  }
  else if (!val && l_limit<sVal){
    sVal-=gap;
  }
  sName.write(sVal);
}
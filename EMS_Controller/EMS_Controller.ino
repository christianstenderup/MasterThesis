#include <DS1803.h>
#include <Wire.h>

DS1803 pot_pulse(0x28);
DS1803 pot_amplitude(0x2C);

int amp;
int width;
int rate;
int amp2;

int FstSource = 0;
int SndSource = 1;
int PulseWidth = 0;
int PulseRate = 1; 

bool turnOnSndSource;

void setup() {
  Serial.begin(9600);

  //Initialize amplitude potentiometers.
  pot_amplitude.setPot(0, FstSource);
  pot_amplitude.setPot(0, SndSource);
  
  //Initialize pulse potentiometers.
  pot_pulse.setPot(0,PulseWidth);
  pot_pulse.setPot(0,PulseRate);

  //Initialize channels.
  pinMode(4,OUTPUT);
  pinMode(5,OUTPUT);
  pinMode(6,OUTPUT);
  pinMode(7,OUTPUT);
  pinMode(8,OUTPUT);
  pinMode(9,OUTPUT);
  digitalWrite(4,LOW);
  digitalWrite(5,LOW);
  digitalWrite(6,LOW);
  digitalWrite(7,LOW);
  digitalWrite(8,LOW);
  digitalWrite(9,LOW);

  int amp = 0;
  int amp2 = 0;
  int width = 0;
  int rate = 0;
  bool turnOnSndSource = false;
}


void loop() { 
  readInput();
}

//Read control signals from serial
void readInput(){
  if (Serial.available() > 0) {
    int commandValue = Serial.parseInt();
    char labelValue = Serial.read();
        
    switch (labelValue) {
      //amplitude
      case 'a' :
        amp = int(commandValue);
        break;
      //amplitude 2
      case 'v' :
        amp2 = int(commandValue);
        break;
      //pulse width  
      case 'w' :
        width = int(commandValue);
        break;
      //pulse rate  
      case 'r' :
        rate = int(commandValue);
        break;
      //channels/relays to open  
      case 'c' :              
        digitalWrite(int(commandValue), HIGH);        
        break;
      case 's' :        
        pot_amplitude.setPot(amp2,SndSource);        
        pot_amplitude.setPot(amp,FstSource);
        pot_pulse.setPot(width,PulseWidth);
        pot_pulse.setPot(rate,PulseRate);
        break;
      case 'e' :
        //turn everything off
        turnOnSndSource = false;
        pot_amplitude.setPot(0, FstSource);
        pot_amplitude.setPot(0, SndSource);
        pot_pulse.setPot(0,PulseWidth);
        pot_pulse.setPot(0,PulseRate);
        digitalWrite(4,LOW);
        digitalWrite(5,LOW);
        digitalWrite(6,LOW);
        digitalWrite(7,LOW);
        digitalWrite(8,LOW);
        digitalWrite(9,LOW);
        break;

      default:
        break;
    }
    
  }
}

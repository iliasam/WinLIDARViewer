# WinLIDARViewer
Windows (C#) utility for testing some LIDARs.  
  
Supported models:
 * **camsense-X1**, see https://github.com/Vidicon/camsense-X1
 * **LDS01RR dev board** (Cullinan): https://github.com/iliasam/LDS01RR_lidar    
 * **Direct LDS01RR**, see protocol here: https://github.com/Roborock-OpenSource/Cullinan  
 If I'm not mistaken, LDS01RR protocol is compatible with LDS02RR, wich is compatible with Neato XV11 protocol.  
 So **LDS02RR** and **Neato XV11** should be compatible too.
  
Features:  
 * Draw a "radar" plot with selectable zoom  
 * Display the distance value for a given direction  
 * Analyze noise information for a given direction, including drawing a histogram  
 ![](https://github.com/iliasam/WinLIDARViewer/blob/master/Pictures/Screen1.png)




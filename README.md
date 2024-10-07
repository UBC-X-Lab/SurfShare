# SurfShare

Repository for the paper: 

Xincheng Huang and Robert Xiao. 2023. SurfShare: Lightweight Spatially Consistent Physical Surface and Virtual Replica Sharing with Head-mounted Mixed-Reality. Accepted and will appear on Proc. ACM Interact. Mob. Wearable Ubiquitous Technol. 7, 4 (December 2023).

The actual project code is in the SS folder (the rest are tests and experiments).

## Dependencies:

### Hardware:

Two Microsoft HoloLens 2 + a computer serving as the server. The HoloLens runs the Unity app, and the server runs node-dss for Web-RTC and the Mirror Unity plugin for shared virtual objects. The two HoloLens and the server should be connected to a local network or internet.

### Software

Microsoft Mixed Reality Web-RTC.

Unity Mirror: This is the plug-in for networking the virtual assets.

OpenCVSharp: for contour detection in the process of creating virtual replicas.

Triangle: for triangulation in the process of creating virtual replicas.

All of these are already pre-built in the Unity project in the SS folder. You should only need to change the IP address for Web-RTC and Mirror Network manager. Note that the windows firewall may block the Web-RTC / Mirror traffic. If you encounter any network-related problems, try turning off the firewall (or more appropriately, allow network access for Unity and WebRTC).

### More details for the software configuration will be provided soon.

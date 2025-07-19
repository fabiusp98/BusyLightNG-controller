# BusyLightNG_Controller
Companion app for the [lightbar](https://git.pinciroli.xyz/fabio.pinciroli/BusyLightNG_Device)

## Working principle
When enabled, the code checks every second what the state of the `LastUsedTimeStop` key of every folder under `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone\NonPackaged` in the Windows registry.
If `LastUsedTimeStop` is 0, then the microphone is in use by that application.

## Lightbar interface
Interface with the lightbar is by way of a 115200 serial port, sending 0 or 1 depending on the color.
All animantions are done on the lightbar microcontroller.

# License
This code is provinded under the GNU AFFERO GENERAL PUBLIC LICENSE version 3, which is available in the repo.
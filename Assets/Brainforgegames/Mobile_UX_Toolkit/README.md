# Mobile UX Toolkit

Mobile UX Toolkit is a lightweight Unity package that provides common user
interface utilities for games and interactive applications, with a focus on
mobile-friendly workflows, performance, and clean architecture.

---

## Table of Contents
1. Requirements  
2. Installation  
3. Setup  
4. Demo Scene  
5. Usage Overview  
   5.1 Toast  
   5.2 Popup  
   5.3 Loading Panel  
   5.4 Internet Availability Checker  
   5.5 Screen Transition  
6. Notes  
7. Support

---

## 1. Requirements
- Unity 2021 or newer
- TextMeshPro (Unity built-in package)

---

## 2. Installation
1. Import the Mobile UX Toolkit package into your Unity project.
2. If prompted, import TextMeshPro Essentials.
3. No third-party dependencies are required.

---

## 3. Setup
1. Open your first scene.
2. Add the following prefabs to the scene:
   - ScreenTransition
   - Toast
   - Popup
   - Loading Panel
   - Internet Availability Checker
3. These prefabs use a singleton-based approach and persist across scenes.

---

## 4. Demo Scene
A demo scene named `MobileUX_Demo` is included in the package.
This scene demonstrates the usage of all available modules and can be used
as a reference during integration and testing.

---

## 5. Usage Overview

### Toast
Used to display short informational messages.
```csharp
Toast.Show("This is a toast message");
```

### Popup
Used to display alert or confirmation dialogs.
```csharp
Popup.ShowAuto("Title", "Description here");
```

### Loading Panel
Used to block user interaction during loading operations.
The loading panel can be shown or hidden globally from anywhere in the project.
```csharp
Loader.Show();
```

### Internet Availability Checker
Used to monitor internet connectivity.
It supports:
	•	Continuous internet checking
	•	On-demand internet checks for specific operations
```csharp
InternetAvailabilityChecker.CheckNow(isOnline =>
                {
                    Debug.LogError("Internet Online: " + isOnline);
                });
```

### Screen Transition
Used to create smooth transitions between scenes or gameplay states.
```csharp
ScreenTransition.CloseCircle();
ScreenTransition.OpenCircle();
```

## 6. Notes
- Designed for both mobile and desktop projects.
- No tweening or external animation libraries are required.
- Clean and minimal API intended for quick integration.
- Feel free to use this asset in your live games.

## 7. Support
For support or questions, contact:
contact@brainforgegames.com
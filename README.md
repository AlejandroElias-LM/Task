# Dungeon Inventory POC

A small 2D retro-style prototype built in 48 hours, focused on testing and showcasing an inventory and item-modifier system.

---

## 🎮 Overview

This project is a simple 2D game where you control a small blue knight exploring a dungeon. You can defeat monsters, collect items they drop, and equip those items inside a grid-based inventory. Items modify your character’s stats and can grant special effects.

The goal of this POC is to demonstrate a functional UI inventory system with stackable stats, item modifiers, and a basic save/load mechanic.

---

## 🧩 Features

### **Inventory System**

* Drag-and-drop grid-based inventory
* Items become active only when placed inside the inventory
* Activated items stack their stats onto the player
* Stats include:

  * **Damage**
  * **Attack Speed** (swing rate)
  * **Range** (weapon scale)

### **Item Modifiers**

Items may include:

* **Positive effects**, such as regenerating health per hit
* **Negative effects**, such as reduced damage

Only equipped (inventory-placed) items apply their modifiers.

### **Combat**

* Simple melee combat using a sword swing
* Enemies drop items on death

### **Save & Load System**

* Saves persistent data inside the **Persistent Data Path**
* Reloading the scene restores the previous save state
* **Only items currently inside the active inventory are saved**

> The file system may trigger Windows warnings since it writes to disk.
> You can check all saving logic in **SaveManager.cs**.

---

## ⌨️ Controls

* **Move:** Arrow Keys or WASD
* **Attack:** Space or Left Mouse Button
* **Interact:** Mouse

---

## 🛠️ Tools Used

* **Unity** (game engine)
* **Aseprite** (custom sprites)
* **Itch.io assets** (additional spritesheets)

---

## 📁 Project Status

This is a **48-hour proof of concept**, so some features are limited:

* No item destruction system
* Only a few buffs implemented, but many more exist in the code for future expansion

---

## 👤 Author

**Alejandro Elias**
Thank you for checking out the project!

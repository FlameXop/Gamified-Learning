# 👻 Gamified Learning — Psychological Horror Examination System

> **Developed by FlamexStudios** | Built in **Unity 6 (URP)**[cite: 1, 5]

An atmospheric, AAA-style examination, study, and cheating mechanic built for a psychological school horror game. This repository houses the core **Curriculum**, **Library Study**, **Tablet Examination**, and **Tactile Cheat-Sheet (Chit)** systems, complete with procedural text decoding, Cinemachine camera transitions, post-processing depth-of-field blur volumes, and dynamic question/distractor generation.

---

## 🎥 System Previews & Showcases

### 1. Classroom Desk & Exam Welcome Sequence
*(Cinemachine camera blend into the desk view + Cyberpunk glitch-in UI transition)*



https://github.com/user-attachments/assets/f5ebd917-a9e4-4963-8c10-1f8bfd11b443




### 2. Library Book Study & Independent Chit Notepad Overlay
*(Cinemachine transition into Gaussian blur volume, glitch-pop page decoding, and simultaneous Chit note-taking)*[cite: 7, 8]




Uploading Gamified Learning - DemoScene_night - Windows, Mac, Linux - Unity 6.3 LTS (6000.3.23f1)_ _DX11_ 2026-10-15 18-58-10.mp4…



---

## ✨ Key Features Built So Far

### 📚 1. Interactive Library Study System (`LibraryBookUIManager` & `BookInteractable`)[cite: 1, 2, 3]
* **Cinematic Inspection:** Interacting (`E` / `Gamepad Y`) with a physical 3D book on library tables triggers a **Cinemachine camera blend** into a dedicated **Gaussian Blur Box Volume**, lifting the 3D book mesh toward the screen before opening the 2D book spread.
* **Single-Source Content Pagination:** Reads directly from a class's `ExamData` ScriptableObject and automatically formats study notes across Left and Right pages[cite: 2, 3, 7].
* **Glitch-Pop Page Transitions:** Turning pages (`Next` / `Previous` UI buttons or `A`/`D` / Arrow Keys) triggers paper-rustling SFX alongside a procedural character-scrambling decode animation[cite: 2, 7].
* **Progression Locks:** Books check the player's current curriculum progress—attempting to read advanced class books before passing earlier exams triggers an **Access Denied** state.
* **Standardized UX:** Clean, button-less exit flow using **`ESC`** (or `Gamepad East`) to put the book down and blend the camera smoothly back to the player[cite: 7, 8].

### 📝 2. Independent Tactile "Chit" (Cheat-Sheet) System[cite: 1, 8]
* **Universal Toggle (`Tab`):** A completely decoupled notepad system that can be pulled out or tucked away at any time—while roaming the school, studying a library book, or actively sitting in an exam[cite: 8, 9].
* **Smooth Pop, Tilt & Fade Animation:** Driven by `CanvasGroup` alpha, scale interpolation, and a subtle `-6°` handheld tilt curve.
* **Smart Input & Layer Locking:**
  * Automatically forces `Chit_Panel` to the front of the Canvas sorting order (`SetAsLastSibling`) so book or tablet text never bleeds through the paper[cite: 8].
  * Automatically freezes player `WASD` movement and `Space` jumping while typing notes into the `TMP_InputField` (`500` character limit)[cite: 8, 9].

### 💻 3. Dynamic Tablet Examination Engine (`ExamUIManager` & `DeskInteractable`)[cite: 1, 2, 9]
* **7-Test Curriculum Architecture:** Supports **5 Classroom Exams** and **2 Extracurricular Tests**:
  * **Audio Spelling Bee (Class 1):** Plays spoken word audio clips (`Replay` supported) and validates player typed input against the `ExamData` answer key[cite: 7].
  * **Auto-Generated Multiple Choice (Classes 2–7):** Requires **zero manual wrong-answer entry**. At runtime, the system shuffles the question pool, pulls up to 10 random questions per run, and automatically generates 3 unique wrong options (distractors) from other answers in the same book.
* **Cyberpunk Glitch-In & Text Decoding:** Both the Welcome Screen and Exam Tablet power on with vertical scale-snapping, positional jitter, and matrix-style character decoding.
* **Global Ghost Awareness Bar:** Wrong answers or getting caught slacking/cheating increment `CurrentAwareness` (`0–100%`), triggering the Ghost death sequence at max capacity.

---

## 🧠 Core Architecture & Script Breakdown

All core scripts reside under the `FlamexStudios.Exam` and `FlamexStudios.Player` namespaces:

| Script | Responsibility |
| :--- | :--- |
| **`ExamData.cs`**[cite: 1, 9] | `ScriptableObject` asset (`Create -> FlamexStudios -> Exam Data`) storing class number, title, study notes, question prompts, correct answers, and audio clips[cite: 3, 5]. |
| **`CurriculumManager.cs`**[cite: 1, 9] | Singleton tracking `currentUnlockedTest` (`1–7`), validating room/book access, and firing Access Denied audio/UI feedback. |
| **`ExamUIManager.cs`**[cite: 1, 9] | Central state machine (`Inactive`, `Transitioning`, `WelcomeScreen`, `InTest`), dynamic MCQ generator, Chit animation controller, and Ghost Awareness tracker. |
| **`LibraryBookUIManager.cs`**[cite: 1, 2, 9] | Singleton managing the 2D `BookPanel` spread, left/right page string building, audio cues, and glitch-pop text transitions[cite: 2]. |
| **`BookInteractable.cs`**[cite: 1, 3, 9] | Attached to 3D library book triggers; handles proximity detection, lock checks, 3D mesh lifting, and Cinemachine priority switching[cite: 3, 5]. |
| **`DeskInteractable.cs`**[cite: 1, 9] | Attached to classroom exam desks; handles sitting transitions, camera blending, and launching the exam sequence. |
| **`PlayerInputHandler.cs`**[cite: 1, 6, 9] | Unity New Input System wrapper routing `Interact`, `ToggleChit`, `Move`, `Jump`, and `Sprint` callbacks[cite: 6, 9]. |

---

## 🛠️ Scene & Inspector Setup Guide

1. **`Game_Managers` (Singleton Root):**[cite: 1, 2, 5]
   * Attach `CurriculumManager`, `ExamUIManager`, and `LibraryBookUIManager`[cite: 1, 2, 5].
2. **`Exam_Canvas` (Main UI Hierarchy):**[cite: 1, 2, 5]
   * `WelcomeScreen_Panel` *(Start Test screen + Title text)*[cite: 1, 2, 5]
   * `Test_Panel` *(Tablet UI containing `MCQ_Container` and `Spelling_Container`)*[cite: 1, 2, 5]
   * `HUD_Panel` *(Awareness Slider & Trust Meter)*[cite: 1, 2, 5]
   * `BookPanel` *(Open book spread with `Left_Page_Text`, `Right_Page_Text`, `Book_Title_Text`, `Page_Number_Text`, and Next/Prev buttons)*[cite: 2, 5, 8]
   * `Chit_Panel` *(Keep at the bottom of `Exam_Canvas` with a `CanvasGroup` and `TMP_InputField`)*[cite: 8]
3. **Adding a New Class Exam & Book:**
   * Right-click in the Project window $\rightarrow$ `Create -> FlamexStudios -> Exam Data` (e.g., `class1`)[cite: 3, 5].
   * Fill in the `Questions` list once—both the Library Book and Classroom Tablet will automatically read from this asset.
   * Assign the asset to the corresponding `BookInteractable` in the library and `DeskInteractable` in the classroom[cite: 3, 5].

---

## 🗺️ Upcoming Roadmap
- [x] Core Examination State Machine & Dynamic Question/Distractor Generator
- [x] Library Book Cinemachine + Blur Volume Study System[cite: 7]
- [x] Independent Animated Chit Notepad System[cite: 8]
- [ ] Populate full 7-Exam Curriculum (`class1` through `class7`)
- [ ] Extracurricular Mechanics: Basketball Court Shooting Test
- [ ] Ghost AI Patrol, Awareness Integration & Cutscene Transitions

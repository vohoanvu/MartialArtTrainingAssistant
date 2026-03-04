This documentation outlines the comprehensive technical solution for solving the **"Identity Drift"** problem in your BJJ analysis application. By combining the **"Smart Frame Picker"** with a **"Face/Body Cluster"** UI, we create a high-confidence visual anchor that guides the downstream AI agents.

---

## 🥋 Project: Identity-Guaranteed BJJ Analysis Pipeline

**Objective:** Eliminate AI confusion between the user (Student) and the opponent by establishing a "Ground Truth" visual reference before full-video processing begins.

### 1. The "Smart Identity Discovery" UX (Frontend)

Instead of processing the entire video file blindly, we guide the user through a two-step "Identity Lock" phase in the React SPA.

* **Step A: The Smart Frame Picker**
* The app extracts 10 high-quality frames from the first 60 seconds of the video (using `ffmpeg`).
* **The User sees:** A horizontal scrollable gallery of these frames.
* **The Action:** The user selects the frame where they (and their opponent) are most clearly visible—usually during the "handshake" or the first standing exchange.


* **Step B: Face/Body Cluster Selection**
* Once a frame is selected, the **Detection Agent** (Backend) identifies all human participants.
* **The User sees:** A "Portrait Grid" of 2–4 square crops extracted from that frame (Student, Opponent, and potentially the Referee).
* **The Action:** The user simply clicks on the portrait that is **THEM**.
* **Final Verification:** A quick toggle asks: *"Confirm your Gi color for this match: [Black] [White] [Blue]"*.



---

### 2. Agentic Orchestration (.NET 8 Backend)

To handle this high-resolution task, the application follows a **Multi-Step Agentic Chain**. This prevents "hallucinations" by specializing each LLM call.

| Agent | Responsibility | Input | Output |
| --- | --- | --- | --- |
| **1. The Detection Agent** | Identify humans and extract crops for the UI. | Selected Frame (Image) | Bounding Boxes + 300x300px Cropped Portraits. |
| **2. The Profiling Agent** | Generate the "Visual Anchor" text description. | User-Selected Crop + Gi Color | "Student: Black Kingz Gi, White Belt 2-stripes, Red Rashguard." |
| **3. The Event Logger** | Map out every technical exchange in the video. | Full Video + Visual Anchor Profile | List of Timestamps, Techniques, and Outcomes. |
| **4. The Head Coach** | Perform deep analysis and suggest drills. | Event Log + Student Level (White Belt) | Structured JSON with Analysis and Drills. |

---

### 3. Solving "Identity Drift" (Technical Logic)

**Identity Drift** happens when the AI "swaps" the players during a scramble. We solve this by passing the **Step 2 (Visual Profile)** into the **Step 3 (Event Logger)** prompt as an immutable constraint.

**The "Anchor" Instruction for the Event Logger:**

> "Reference the attached **Identity Crop** (input_file_0.png). This is the 'Student'. The student is wearing a [Black] Gi with [Specific Patch]. Even during high-speed scrambles or when the Student is on the bottom, always verify identity using the [Specific Patch] and [Belt Detail] before logging a technique."

---

### 4. Technical Implementation Summary

* **Image Processing:** Use **Magick.NET** or **ImageSharp** in .NET 8 to handle the cropping logic based on the normalized coordinates  returned by Vertex AI.
* **Storage:** The user's "Identity Crop" is stored in Postgres as a Base64 string or a GCS link. This allows the user to "re-use" their identity profile for future match uploads from the same tournament without re-selecting themselves.
* **Inference:** By providing a **High-Resolution Image** alongside the **Compressed Video**, you give the Gemini model's attention mechanism a specific pattern to "search" for in every frame, virtually eliminating player confusion.

### Next Steps for Development

Generate the **OpenAPI (Swagger) specification** for the `IdentityDetection` and `VideoAnalysis` endpoints so we can begin building the .NET controllers.


--------------------------------
*** Updated fix for Step 1 (The "Smart Identity Discovery" UX) ***

This section outlines the architectural blueprint for an **Agentic Identity-Lock Workflow** designed to solve "Identity Drift" in combat sports video analysis. By utilizing a multi-step chain, we transition from high-level uncertainty to a "pinned" visual profile that ensures the AI analyzes the correct athlete throughout a complex 3GB BJJ match.

---

# 🥋 Technical Design: BJJ Identity-Lock Pipeline

## 1. Architectural Overview

The system is built as a **Sequential Agentic Chain**. Instead of a single "black box" prompt, the task is decomposed into specialized micro-services (Agents) that progressively refine data.

| Phase | Agent | Input | Technical Output |
| --- | --- | --- | --- |
| **I. Discovery** | **Scout Agent** | Video (60s) + Text ID | Ranked Frame Indices (Top 3) |
| **II. Segmentation** | **Identity Agent** | Selected Frame | JSON Bounding Boxes + Visual DNA |
| **III. Tracking** | **Event Logger** | Video + Anchor Frame + DNA | Timestamped Technical Log |
| **IV. Analysis** | **Coach Agent** | Technical Log | Final JSON (Analysis & Drills) |

---

## 2. Phase I: The Scout Agent (Auto-Selection)

To minimize user friction, the **Scout Agent** pre-screens the video for high-information-density frames.

* **Logic:** The backend extracts 20 frames (1 frame/3s) from the first minute of the match.
* **Prompting Strategy:** Vertex AI is asked to rank these frames based on the user's `Student_Identifier` (e.g., "Fighter in Blue Gi").
* **Success Metric:** The agent prioritizes frames with minimal motion blur and clear separation between athletes (e.g., the pre-match handshake).

---

## 3. Phase II: The Identity Agent & "Visual DNA"

This is the "Heart" of the solution. It combines **Object Detection** with **Feature Extraction** in a single multimodal call.

### A. The Bounding Box Request

The agent analyzes the user-selected frame and returns a structured JSON object containing all "Human" entities.

**Vertex AI System Instruction:**

> "Identify all human participants in the frame. Return a JSON array of objects, each containing:
> 1. `box_2d`: [ymin, xmin, ymax, xmax]
> 2. `label`: Gi Color and visible rank
> 3. `visual_dna`: A 2-sentence description of unique markers (patches, hair color, tape, etc.)"
> 
> 

### B. The .NET 8 Cropping Service

Your API receives the normalized coordinates . Using **ImageSharp**, the backend performs a localized crop of each participant.

* **User Interaction:** The React UI presents these crops in a **"Who are you?"** grid.
* **Result:** The user selects the crop that is them. This selected image becomes the **Permanent Anchor**.

---

## 4. Phase III: The Event Logger (Solving Identity Drift)

The final analysis call is "grounded" by the Anchor. You are no longer asking the AI to find "a person"; you are asking it to track a specific **Visual Signature**.

### The "Multimodal Payload"

The final call to the **Event Logger Agent** includes:

1. **The Video File:** (Optimized/Compressed for inference).
2. **The Anchor Crop:** A high-resolution JPG of the student.
3. **The Visual DNA:** The text profile (e.g., "Student has a red patch on the right shoulder and white finger tape").

### Why this stops Drift:

By providing a static reference image (The Anchor) alongside the video, the LLM’s **cross-modal attention** is forced to "feature-match" the pixels of the anchor against the frames of the video. Even if the student is upside down or in a pile-up, the AI looks for the specific "Red Patch" DNA established in Phase II.

---

## 5. Summary of the "Permanent Text Profile"

The output of Phase II is stored in your Postgres database as a `VisualProfile` entity.

```json
{
  "match_id": "bjj_001",
  "student_anchor_image_url": "gcs://bucket/anchor_crop_user1.jpg",
  "visual_dna": "Student wears a black 'Kingz' brand Gi, white belt with 2 stripes. Notable for long blonde hair tied in a bun and blue rashguard sleeves visible under the Gi.",
  "gi_color": "Black"
}

```

This JSON object is injected into every subsequent analysis prompt, acting as the "Source of Truth" for the AI Coach.

---

### Final Implementation Checklist

1. [ ] **FFmpeg Integration:** Setup .NET task to extract frames on upload.
2. [ ] **JSON Schema Enforcement:** Configure Vertex AI `response_mime_type` to `application/json` for Step 2.
3. [ ] **Coordinate Scaling:** Implement the math to convert  coordinates to local image pixels.
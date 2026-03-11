# 🥋 Technical Design: Zero-Click BJJ Identity-Lock Pipeline

This document outlines the finalized architecture for a friction-less, agentic video analysis pipeline designed to eliminate **Identity Drift**. By utilizing a multi-step chain and temporal chunking, the system ensures the AI maintains a "Ground Truth" visual anchor of the student throughout complex grappling matches.

---

## 1. Architectural Overview

The system transitions from a single, high-level "black box" prompt to a series of specialized micro-services (Agents).

| Phase | Agent | Persona | Technical Task |
| --- | --- | --- | --- |
| **I. Discovery** | **Auto-Profiler** | Forensic Video Analyst | Generate immutable "Visual DNA" from first 15s. |
| **II. Segmentation** | **Backend Service** | Deterministic Code | Slice video into 45s chunks with 3s overlap. |
| **III. Tracking** | **Event Logger** | Play-by-Play Commentator | Log techniques using Visual DNA grounding per chunk. |
| **IV. Merging** | **.NET Service** | Deterministic Code | Adjust timestamps and deduplicate overlapping events. |
| **V. Analysis** | **Head Coach** | IBJJF Black Belt | Synthesize merged logs into coaching drills/feedback. |

---

## 2. Phase I: The Auto-Profiler (Zero-Click Discovery)

This agent identifies the student based on the initial user description and creates a detailed text-based anchor.

### System Instruction

> "You are a Forensic Video Analyst. Your sole task is to identify a specific athlete and document their unique visual characteristics. Ignore all technical grappling actions."

### Sample Prompt

> "Watch the first 15 seconds of this video. Locate the student described as '{studentIdentifier}'. Generate a 'Visual DNA' profile. Look for: Gi color/brand, belt rank/stripes, hair color/style, specific patches, rashguard colors, and athletic tape on fingers or joints. Return ONLY the description."

---

## 3. Phase II & III: Chunking and the Event Logger

To prevent **Identity Drift**, the video is processed in 45-second segments. Each segment is "grounded" by the Visual DNA.

### Event Logger System Instruction

> "You are a meticulous grappling play-by-play logger. You must strictly track the student matching the provided 'Visual DNA' profile. Do not swap identities during scrambles. Focus on positional changes and technique execution."

### Event Logger JSON Schema

```json
{
  "type": "object",
  "properties": {
    "events": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "timestamp": { "type": "string", "description": "MM:SS relative to chunk" },
          "actor": { "type": "string", "enum": ["Student", "Opponent"] },
          "technique_name": { "type": "string" },
          "position": { "type": "string", "enum": ["Standing", "Guard", "Side Control", "Mount", "Back", "Turtle"] },
          "outcome": { "type": "string" }
        },
        "required": ["timestamp", "actor", "technique_name", "position"]
      }
    }
  }
}

```

---

## 4. Phase IV: Implementation of "Time-Shift" Logic

The merging of chunked data is handled by a **deterministic .NET service**, not an LLM, to ensure mathematical accuracy.

* **Timestamp Re-basing:** For each chunk $n$, the service adds the start time of that chunk (minus overlap) to every event timestamp.
* *Formula:* $T_{actual} = T_{chunk} + (ChunkIndex \times (45s - 3s))$.


* **Deduplication:** The service scans the 3-second overlap windows. If an event (e.g., "Triangle Choke") appears at the end of Chunk 1 and the start of Chunk 2 with identical metadata, the service merges them into a single entry.
* **Sorting:** All re-based events are sorted chronologically into a single "Master Match Log".

---

## 5. Phase V: The Head Coach (Synthesis)

The Coach Agent consumes the Master Match Log (text only) to provide high-level insights.

### Head Coach System Instruction

> "You are an expert Brazilian Jiu-Jitsu Head Coach. Analyze the provided match log to identify patterns in the student's performance. Prioritize score-losing technical errors based on IBJJF rules."

### Head Coach JSON Schema

```json
{
  "type": "object",
  "properties": {
    "match_summary": { "type": "string" },
    "key_strengths": {
      "type": "array",
      "items": { "type": "object", "properties": { "title": { "type": "string" }, "explanation": { "type": "string" } } }
    },
    "critical_weaknesses": {
      "type": "array",
      "items": { 
        "type": "object", 
        "properties": { 
          "title": { "type": "string" }, 
          "timestamp_reference": { "type": "string" }, 
          "explanation": { "type": "string" } 
        } 
      }
    },
    "prescribed_drills": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "drill_name": { "type": "string" },
          "instructions": { "type": "string" },
          "goal": { "type": "string" }
        }
      }
    }
  }
}

```

---

## 6. Technical Implementation Summary

* **VLM:** Gemini 3.1 (Vertex AI).
* **Backend:** .NET 8 for video orchestration and JSON merging.
* **Storage:** Postgres for storing the "Visual DNA" and final analysis reports.
* **Benefit:** Zero user friction with significantly reduced hallucination and identity swapping.


## A Final Pro-Tip for 2026: Thought Signatures
In your .NET implementation, I strongly recommend utilizing the new Thought Signatures feature for the Event Logger. When you process Chunk 1, the model can emit a `thoughtSignature`. If you pass that signature into the request for Chunk 2, it helps the model maintain the same "reasoning flow" and identity-tracking logic across the temporal boundary. (MORE DETAILS BELOW...)



## More Details on Thought Signatures
The **Thought Signature** is a new stateful reasoning mechanism in the Gemini 3 series that replaces the "stateless" nature of previous models.

In a standard API call, the model starts its "thinking" from scratch every time. By using Thought Signatures, you are passing an encrypted, opaque token string that encapsulates the model's internal hidden states and intermediate computation results from the previous step.

For your BJJ pipeline, this is the "glue" that prevents the AI from losing the "why" behind its tracking logic as it moves from one 45-second video chunk to the next.

### 1. How the "Relay" Works (Conceptual)

Think of it like a relay race where the "baton" is the model's internal logic about who the student is.

* **Step 1 (Chunk 1):** You send the video chunk + Visual DNA. The model thinks: *"I see the red patch on the left; that's the student."* Along with its JSON output, it returns a **`thoughtSignature`**.
* **Step 2 (Chunk 2):** In your next request, you send the second video chunk. **Crucially**, you include the `thoughtSignature` from Step 1 in the message history.
* **The Result:** The model doesn't just re-read your "Visual DNA" prompt; it **resumes** the exact neural state it was in at the end of Chunk 1. It "remembers" the specific lighting, the angle of the red patch, and the student's movement patterns.

---

### 2. Implementation Rules for 2026

Since you are using the .NET SDK, here is the technical behavior you need to implement:

* **Capture the Signature:** The `thoughtSignature` is found in the `Parts` array of the model's response. For Gemini 3 models, it is mandatory to include this if you are using **Function Calling**, but highly recommended for **Text/JSON** workflows to maintain reasoning quality.
* **Strict History Placement:** You must return the signature in the exact message part where it was received when building the conversation history for the next chunk.
* **The "Current Turn" Rule:** The API enforces strict validation on signatures within the "current turn" of an agentic workflow. If you are performing a multi-step analysis within a single chunk, missing a signature will trigger a **400 Bad Request** error.

---

### 3. Impact on Your Pipeline

Using signatures across your temporal chunks solves three major pain points:

1. **Context Continuity:** It preserves the reason *why* the model identified a specific person as the student during a scramble.
2. **Mitigates "Context Rot":** Even if your total context window is large (1M+ tokens), providing the signature helps the model focus on the *relevant* reasoning path rather than getting lost in the "noise" of previous frames.
3. **High-Fidelity Tracking:** In 2026, setting `thinking_level` to **HIGH** on your Pro calls enables **Deep Think Mini** capabilities, which generates significantly more complex thought signatures for superior causal reasoning.
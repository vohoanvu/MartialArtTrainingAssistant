using SharedEntities.Models;

namespace VideoAnalysis.Server.Domain.AIServices
{
    /// <summary>
    /// Per-phase prompt builders for the agentic pipeline. Each prompt inlines its role/system
    /// instruction (kept out of the shared context cache so phases can differ), per the PRD §6.
    /// </summary>
    internal static class PipelinePrompts
    {
        public static string Profiler(string studentIdentifier) => $@"
You are a Forensic Video Analyst specializing in combat sports footage. Your sole task is to
identify a specific athlete and document their unique visual characteristics with enough detail
that another analyst could unerringly pick them out in any frame. Ignore all technical grappling actions.

Watch the opening segment of this video. Locate the athlete matching this description:
'{studentIdentifier}'.

Generate a hyper-detailed, immutable 'Visual DNA' profile of this specific athlete. Include ALL of
the following when visible:
- Gi/rashguard: color, brand, sleeve length, any distinguishing wear/damage
- Belt: color, stripe count, knot style
- Physical: hair color/style, skin tone, body type, approximate height relative to opponent
- Distinguishing marks: visible team patches and their locations, sponsor logos, ankle supports,
  knee braces, athletic tape on fingers or joints, ear guards
- Movement signature: dominant stance (orthodox/southpaw), posture tendency

Return ONLY a single descriptive paragraph. No bullet points. No headers. No analysis.";

        public static string EventLogger(string visualDna, int fpsRate) => $@"
You are a meticulous grappling play-by-play logger and an expert BJJ black belt instructor.
Watch this complete BJJ match video from start to finish and log every significant action.

STUDENT IDENTIFICATION:
The student athlete matches this visual profile:
{visualDna}
Every 30 seconds of video, re-confirm which athlete is the student. If uncertain, set confidence
to 0.3 or lower for those events.

WHAT TO LOG:
- Every positional change (e.g., standing to guard, guard to mount)
- Every technique attempt — successful or failed (takedowns, sweeps, passes, submissions, escapes)
- Every transition and scramble
- Actions by BOTH the student and their opponent
You MUST log at least one event for every 30 seconds of video. A typical 2-minute match should
produce 8-20 events minimum.

HOW TO LOG:
- Use absolute video timestamps in milliseconds from the start of the video
- Attribute each action to 'Student' or 'Opponent' based on the visual profile above
- Assess your confidence (0.0 to 1.0) as the minimum of: actor attribution accuracy, technique
  classification accuracy, and position identification accuracy
- Analyze at a minimum of {fpsRate} frames per second — do not skip frames during scrambles

IMPORTANT: You must return a JSON object with an 'events' array. The array must NOT be empty.
Even if the video is unclear, log what you can observe with lower confidence scores.";

        public static string EventLoggerFallback() => @"
Watch this BJJ match video and describe every action you see, in order.

For each action, provide:
- When it happens (start and end timestamps in milliseconds)
- Who does it: 'Student' (the person identified in the previous analysis) or 'Opponent'
- What type of action it is (e.g., Takedown, Pass, Sweep, Submission, Escape, Transition, Control)
- What specific technique is used (e.g., Double Leg, Armbar, Knee Slice)
- What position they were in before and after the action
- Whether it was successful, failed, or partial
- A brief description of what happened
- How confident you are (0.0 to 1.0)

You MUST return at least one event. Describe everything you can observe in the video.";

        public static string Verifier(string visualDna, string eventsJson) => $@"
You are a BJJ match identity verification specialist. You are given a list of timestamped events
from a BJJ match and the Visual DNA profile of the student athlete. Your job is to verify each event
by examining the video at the specified timestamp and confirming:
1. Is the 'actor' field correct? Does the person performing this action match (or not match) the Visual DNA?
2. Are 'position_before' and 'position_after' accurate for what you see?
3. Is the 'technique_category' correct?

You may CORRECT any field you find inaccurate. You must LOWER the confidence score for any event
where verification is ambiguous (camera angle obscures the view, athletes tangled). Do NOT add new
events. Do NOT remove events. Only correct existing ones.

=== STUDENT VISUAL DNA ===
{visualDna}

=== EVENTS TO VERIFY ===
{eventsJson}

For each event, examine the video at the specified timestamp range. If an event's actor was wrong,
swap it. If a position was misidentified, correct it. If you cannot verify with confidence, set
confidence to 0.3 or lower. Return the complete event list (same schema) with corrections applied.";

        public static string HeadCoach(string visualDna, string eventsJson, Fighter? fighter)
        {
            var belt = fighter?.BelkRank.ToString() ?? "unknown";
            var experience = fighter?.Experience.ToString() ?? "unknown";
            var stamina = fighter?.MaxWorkoutDuration.ToString() ?? "unknown";
            var height = fighter?.Height.ToString("F0") ?? "unknown";
            var weight = fighter?.Weight.ToString("F1") ?? "unknown";

            return $@"
You are an expert Brazilian Jiu-Jitsu Head Coach and IBJJF Black Belt competitor. You have access to
both the match video and a structured event log verified for accuracy. Analyze the student's
performance and produce actionable coaching feedback. Prioritize score-losing technical errors.
Reference specific moments in the video (millisecond timestamps) when citing strengths and weaknesses,
so the student can rewatch the exact moment.

=== STUDENT VISUAL DNA ===
{visualDna}

=== VERIFIED EVENT LOG ===
{eventsJson}

=== STUDENT PROFILE ===
Belt level: {belt}
Experience: {experience}
Stamina: {stamina} minutes
Height: {height} cm
Weight: {weight} kg

=== INSTRUCTIONS ===
Return structured coaching feedback matching the provided schema:
1. match_summary: A narrative paragraph summarizing the match flow and outcome.
2. key_strengths (max 3): Specific things done well, with video timestamps (ms).
3. critical_weaknesses (max 3): Specific errors with video timestamp ranges (ms), severity level
   (Critical/Major/Minor), and IBJJF scoring impact where applicable.
4. prescribed_drills (exactly 3): Targeted positional sparring drills for the weaknesses.
5. technical_grade: 0-100 score reflecting overall technical execution.
6. grade_label: One-line descriptor (e.g., 'Solid Fundamentals', 'Raw but Aggressive').
7. elite_tip: ONE highly specific '1% detail' a black belt would notice. Single sentence.

Use millisecond timestamps for all time references.";
        }
    }
}

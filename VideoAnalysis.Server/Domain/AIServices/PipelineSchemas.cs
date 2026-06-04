using System.Text.Json.Nodes;

namespace VideoAnalysis.Server.Domain.AIServices
{
    /// <summary>
    /// Vertex <c>responseSchema</c> definitions, mirroring the proven MyCoach mobile pipeline
    /// (validated with real uploads). Note: <c>technique_category</c> / <c>position_*</c> use a
    /// description hint rather than a hard <c>enum</c> — this keeps the taxonomy free-evolving and
    /// avoids Vertex rejecting near-miss values; only <c>actor</c>/<c>outcome</c>/<c>severity</c> are
    /// hard enums. Parsed from raw JSON so field names reach Vertex verbatim.
    /// </summary>
    internal static class PipelineSchemas
    {
        public static JsonNode EventsSchema() => JsonNode.Parse("""
        {
          "type": "OBJECT",
          "required": ["events"],
          "properties": {
            "events": {
              "type": "ARRAY",
              "description": "List of all match events observed in the video, in chronological order",
              "items": {
                "type": "OBJECT",
                "required": ["start_timestamp_ms","end_timestamp_ms","actor","technique_category","technique_name","position_before","position_after","outcome","actions_description","confidence"],
                "properties": {
                  "start_timestamp_ms": { "type": "INTEGER", "description": "Absolute milliseconds from start of video when this action begins" },
                  "end_timestamp_ms": { "type": "INTEGER", "description": "Absolute milliseconds from start of video when this action ends" },
                  "actor": { "type": "STRING", "description": "Who performs this action", "enum": ["Student","Opponent"] },
                  "technique_category": { "type": "STRING", "description": "Category of technique. Use one of: Takedown, Submission, Sweep, Pass, Escape, Transition, Control, Defense, GuardPull, Scramble, StandUp, GripFight" },
                  "technique_name": { "type": "STRING", "description": "Specific technique name, e.g. 'Double Leg', 'Armbar from Guard', 'Knee Slice Pass'" },
                  "position_before": { "type": "STRING", "description": "Position at the start of this action. Use one of: Standing, OpenGuard, ClosedGuard, HalfGuard, SideControl, Mount, BackControl, Turtle, KneeOnBelly, NorthSouth, FiftyFifty, Crucifix, Scramble" },
                  "position_after": { "type": "STRING", "description": "Position at the end of this action. Use one of: Standing, OpenGuard, ClosedGuard, HalfGuard, SideControl, Mount, BackControl, Turtle, KneeOnBelly, NorthSouth, FiftyFifty, Crucifix, Scramble" },
                  "outcome": { "type": "STRING", "description": "Result of this action", "enum": ["Successful","Failed","Partial","Countered","InProgress"] },
                  "guard_type": { "type": "STRING", "nullable": true, "description": "Specific guard variant if position is a guard (e.g. 'De La Riva', 'Spider', 'Butterfly'). Null when not in guard." },
                  "submission_type": { "type": "STRING", "nullable": true, "description": "Specific submission name when technique_category is Submission (e.g. 'Rear Naked Choke', 'Armbar'). Null otherwise." },
                  "actions_description": { "type": "STRING", "description": "Free-text description of what happened during this event" },
                  "confidence": { "type": "NUMBER", "description": "Confidence score 0.0-1.0, the minimum of actor attribution, technique classification, and position identification confidence" }
                }
              }
            }
          }
        }
        """)!;

        public static JsonNode CoachingReportSchema() => JsonNode.Parse("""
        {
          "type": "OBJECT",
          "required": ["match_summary","key_strengths","critical_weaknesses","prescribed_drills","technical_grade","grade_label","elite_tip"],
          "properties": {
            "match_summary": { "type": "STRING" },
            "technical_grade": { "type": "NUMBER" },
            "grade_label": { "type": "STRING" },
            "elite_tip": { "type": "STRING" },
            "key_strengths": {
              "type": "ARRAY",
              "items": {
                "type": "OBJECT",
                "required": ["title","explanation","timestamp_start_ms","timestamp_end_ms"],
                "properties": {
                  "title": { "type": "STRING" },
                  "explanation": { "type": "STRING" },
                  "timestamp_start_ms": { "type": "INTEGER" },
                  "timestamp_end_ms": { "type": "INTEGER" }
                }
              }
            },
            "critical_weaknesses": {
              "type": "ARRAY",
              "items": {
                "type": "OBJECT",
                "required": ["title","explanation","timestamp_start_ms","timestamp_end_ms","severity"],
                "properties": {
                  "title": { "type": "STRING" },
                  "explanation": { "type": "STRING" },
                  "timestamp_start_ms": { "type": "INTEGER" },
                  "timestamp_end_ms": { "type": "INTEGER" },
                  "severity": { "type": "STRING", "enum": ["Critical","Major","Minor"] },
                  "scoring_impact": { "type": "STRING", "nullable": true }
                }
              }
            },
            "prescribed_drills": {
              "type": "ARRAY",
              "items": {
                "type": "OBJECT",
                "required": ["drill_name","instructions","goal"],
                "properties": {
                  "drill_name": { "type": "STRING" },
                  "instructions": { "type": "STRING" },
                  "goal": { "type": "STRING" }
                }
              }
            }
          }
        }
        """)!;
    }
}

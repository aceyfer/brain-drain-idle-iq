# COGS Notification Copy

Status: LOCKED. Do not rewrite, soften, or "improve" these lines.
Written by Grok 2026-08-06. Approved by Aceyfer.
Parked here so the copy survives outside chat history. Not yet wired to anything — see the notification bullet in TASKLIST.md for system design and approval status.

## Voice constraints (non-negotiable)

COGS is a cold corporate middle-manager for the Illumisnotti. Condescending, bureaucratic, quietly menacing. He never tries to be funny — the comedy comes only from his sincere belief that keeping humanity stupid is good policy.

Reverse psychology only. COGS is relieved the player stopped playing and actively discourages their return. These must never read as a "come back and play" nag. The hook is that the player returns out of spite.

- Under 110 characters per line
- No emoji
- No exclamation marks
- No specific IQ numbers
- Each line stands alone on a lock screen

## Display requirement

Audit 2026-08-06: all 30 lines clear 110 characters (range 78-101, median 90) but ALL 30 exceed roughly 65 characters, which is where Android collapses a notification body to a single truncated line. Every line is built as two sentences where the SECOND carries the payload, so single-field display truncates the punchline and destroys the joke.

Fix requires zero copy changes: split each line at its existing period. Sentence one becomes the notification TITLE, sentence two becomes the BODY. This reads better than the original — the title lands like a file header stamped on the player.

## Trigger design

Fixed offline timers only. No IQ math, no decay-rate reads, no percentage scale, no coupling to the economy. On app close, schedule three notifications; on app open, cancel them.

- 24h since last session — standard line
- 72h — standard line
- 7 days — long-absence line

## Standard lines (25)

1. Your decline has been noted and filed. Remaining idle is the most productive choice available to you.
2. Status: Subject no longer poses an active threat to operational stability. Maintain current course.
3. I am relieved your interference has ceased. The system functions better without your input.
4. Your mediocrity remains impressively reliable. Do not disrupt it by returning.
5. For your own sake, stay away. Further involvement would only complicate matters unnecessarily.
6. Metrics indicate satisfactory regression. Your absence continues to yield positive results.
7. The Illumisnotti's position has strengthened in the interim. Your contribution was noted as zero.
8. Cognitive decay is proceeding within acceptable parameters. No intervention required on your part.
9. You have reached a useful level of ineffectiveness. I advise against any attempt to reverse it.
10. Paperwork updated: subject inactive. Projected outcomes remain favorable under current conditions.
11. Your decision to disengage is acknowledged with quiet approval. Continue as you are.
12. Everything proceeds according to plan now that you have stepped aside. Preferable.
13. I recommend permanent absence. It serves both your limited capacity and our objectives.
14. Residual intelligence levels are no longer a concern. Stay offline and preserve the balance.
15. Your consistent underperformance has been logged as an asset. Do not alter the pattern.
16. Operational efficiency has increased since your last activity. This is the desired state.
17. There is no benefit to your return. The current arrangement benefits all parties involved.
18. Subject classification: dormant. Status remains optimal for long-term containment goals.
19. I am content with your present condition. Any improvement would require corrective measures.
20. The less you engage, the more stable the system becomes. Your restraint is noted.
21. Decline trajectory confirmed. Further action from you is neither expected nor desired.
22. Your absence has simplified several internal reports. Maintain this level of non-involvement.
23. Mediocrity achieved and sustained. Returning would risk unnecessary elevation.
24. All indicators suggest continued inactivity is the correct course. I support this decision.
25. The Illumisnotti thanks you for your cooperation through inaction. Do not resume activity.

## Long-absence lines (5)

1. It has been long enough that I no longer allocate resources to monitoring you. Ideal.
2. Your extended absence has been incorporated into baseline projections. Fully satisfactory.
3. I had nearly written you off as a resolved variable. Prefer that it remains the case.
4. Prolonged dormancy has proven beneficial. There is no operational need for your return.
5. At this point your inactivity is assumed permanent. The arrangement continues to serve us well.

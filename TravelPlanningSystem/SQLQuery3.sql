-- Fix Activity Session End Time
-- EndTime = StartTime + Activity DurationHours

UPDATE s
SET s.EndTime =
    CAST(
        DATEADD(
            MINUTE,
            CAST(a.DurationHours * 60 AS INT),
            CAST(s.StartTime AS datetime)
        )
        AS time
    )
FROM dbo.ActivitySessions AS s
INNER JOIN dbo.Activities AS a
    ON s.ActivityId = a.ActivityId;

-- Check the result
SELECT
    a.ActivityName,
    a.DurationHours,
    s.SessionDate,
    s.StartTime,
    s.EndTime
FROM dbo.ActivitySessions AS s
INNER JOIN dbo.Activities AS a
    ON s.ActivityId = a.ActivityId
ORDER BY
    a.ActivityName,
    s.SessionDate,
    s.StartTime;
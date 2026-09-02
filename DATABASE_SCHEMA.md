# LearnAble Firestore data design

This project uses Firebase Cloud Firestore, which stores data in **collections** rather than SQL tables.

## `users` collection

Document ID: `userId`

| Field | Type | Notes |
| --- | --- | --- |
| `userId` | string | Unique Firebase Authentication user ID. |
| `username` | string | User-facing unique username. |
| `schoolId` | string | School identifier. |
| `name` | string | Full name. |
| `role` | string | `student`, `parent`, `teacher`, or `admin`. |
| `updatedAt` | timestamp | Set by Firestore. |

Passwords must **not** be kept in Firestore. Firebase Authentication stores and verifies the password securely; the user's Firebase Auth ID becomes `userId` in this collection.

## `activityScores` collection

Each document is one successful activity completion.

| Field | Type | Notes |
| --- | --- | --- |
| `userId` | string | Links the result to `users/{userId}`. |
| `activityName` | string | Unity scene name that completed successfully. |
| `score` | number | Score from 0 through 100. |
| `correctAnswers` | number | Correct answers, matches, or traced lines. |
| `totalItems` | number | Total answers, matches, or lines. |
| `completedAt` | timestamp | Set by Firestore. |

`LearningDataStore.cs` provides `CreateOrUpdateUser`, `SetCurrentUser`, and `RecordSuccessfulActivity` for these collections.

## Teacher-entered academic grades

Grades are stored on the existing `users/{documentId}` student profile, independently of game scores. Selection in GradesScene uses the same document ID as student login.

| Field | Type | Notes |
| --- | --- | --- |
| `academicGrades` | map of numbers | Keys such as `ENGLISH_1ST`, `MATH_2ND`, `SCIENCE_4TH`; only entered quarters exist. |
| `teacherRemark` | string | Latest nonempty teacher note entered with a grade. An empty optional input preserves the previous note. |
| `gradesUpdatedAt` | timestamp | Server timestamp for the last grade edit. |

InputGradeManager updates only the selected subject/quarter and optional note, preserving other grades and profile fields. Both StudentGradesScene and StudentProfileScene's GradesPanel listen to this same profile through StudentGradesView.

Subject averages use entered quarters only; the general average is the equally weighted mean of available subject averages, calculated before rounding. Missing grades and averages stay blank. A saved zero is a grade, not a missing value. Averages display up to two decimal places. While quarters are incomplete, these are running averages.

Automatic subject remarks use unrounded averages: below 75 = Failed (red), 75 to below 85 = Good, 85 to below 90 = Very Good, and 90 through 100 = Excellent. The structured firstName and lastName fields supply the report name without the middleName field.

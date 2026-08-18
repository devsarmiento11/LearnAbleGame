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

# Reusing the drag-answer game

MathGame3Level1 is already wired. It compares 3 fish with 5 fish; Choice1 (`less`) is correct. The single answer slot is one question, worth 100 points.

For another level, duplicate the scene and replace the pictures without changing the scripts:

1. Put `DragAnswerChoice` on each draggable UI image. Set its **Answer Id** to a meaningful value, such as `less`, `greater`, `equal`, or any matching category.
2. Put `DragAnswerZone` on each answer slot. Set **Accepted Answer Id** to the exact correct ID. The slot needs a UI Graphic with Raycast Target enabled.
3. On `DragAnswerLevel`, assign every choice once, every zone once, and the existing GameTimer. IDs are independent of object names and pictures.
4. Connect Done to `DragAnswerLevel.Done` and Undo to `DragAnswerLevel.UndoLastMove`. Remove old scoring/undo handlers from those buttons.
5. Set the pass percentage and result scene names, and add the new scene to the build scene list.

Choices dropped outside a slot return home. Dropping onto an occupied slot returns the old choice home. Undo restores the complete previous arrangement, including replacements. Mouse and touch use Unity's EventSystem; one drag is accepted at a time.

Done calculates `round(correct occupied slots / total slots * 100)`, including unanswered slots in the denominator. Repeated moves never add points. Finishing records the actual correct count and slot count once through ScoreManager. Timeout records the current answers and opens the failed result scene. Try Again uses the existing PreviousGameScene flow.

Keep only one DragAnswerLevel and GameTimer per activity scene. Do not attach the older DraggableWord/DropZone components to these same choices or zones.

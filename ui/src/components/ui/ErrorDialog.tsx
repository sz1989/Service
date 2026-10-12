import styles from "./ErrorDialog.module.css";
import { DraggableDialog } from "./DraggableDialog";

interface ErrorDialogProps {
    message: string
    onClose: () => void
}

export function ErrorDialog({ message, onClose }: ErrorDialogProps) {
    return (
        <DraggableDialog title='Error' onClose={onClose}>
            <div className={styles.body}>
                <p role='alert' className={styles.message}>{message}</p>
                <div className={styles.buttons}>
                    <button type='button' onClick={onClose}>Ok</button>
                </div>
            </div>
        </DraggableDialog>
    );
}

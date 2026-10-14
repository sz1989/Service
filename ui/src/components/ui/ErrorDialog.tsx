import styles from "./ErrorDialog.module.css";
import { DraggableDialog } from "./DraggableDialog";

interface ErrorDialogProps {
    title?: string 
    message: string
    onClose: () => void
}

export function ErrorDialog({ title = 'Error', message, onClose }: ErrorDialogProps) {
    return (
        <DraggableDialog title={title} onClose={onClose}>
            <div className={styles.body}>
                <p role='alert' className={styles.message}>{message}</p>
                <div className={styles.buttons}>
                    <button type='button' onClick={onClose}>Ok</button>
                </div>
            </div>
        </DraggableDialog>
    );
}

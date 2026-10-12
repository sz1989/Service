import styles from "./ConfirmDialog.module.css";
import { DraggableDialog } from "./DraggableDialog";

interface ConfirmDialogProps {
    title: string
    message: string
    confirmLabel: string
    onConfirm: () => void
    onCancel: () => void
}

export function ConfirmDialog({ title, message, confirmLabel, onConfirm, onCancel }: ConfirmDialogProps) {
    return (
        <DraggableDialog title={title} onClose={onCancel}>
            <div className={styles.body}>
                <p className={styles.message}>{message}</p>
                <div className={styles.buttons}>
                    <button type='button' className='button-danger' onClick={onConfirm}>{confirmLabel}</button>
                    <button type='button' className='button-secondary' onClick={onCancel}>Cancel</button>
                </div>
            </div>
        </DraggableDialog>
    );
}

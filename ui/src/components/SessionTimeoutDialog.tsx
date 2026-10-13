import styles from "./SessionTimeoutDialog.module.css";
import { DraggableDialog } from "./ui/DraggableDialog";

interface SessionTimeoutDialogProps {
    /** Whole seconds until the user is signed out. */
    secondsLeft: number
    /** Keep the session going. Also what the × button and Esc do. */
    onStay: () => void
    onSignOut: () => void
}

// "Are you still there?" shown shortly before an inactivity logout.
export function SessionTimeoutDialog({ secondsLeft, onStay, onSignOut }: SessionTimeoutDialogProps) {
    return (
        <DraggableDialog title='Are you still there?' onClose={onStay}>
            <div className={styles.body}>
                <p role='timer' className={styles.message}>
                    You will be signed out in {secondsLeft} {secondsLeft === 1 ? 'second' : 'seconds'} because of inactivity.
                </p>
                <div className={styles.buttons}>
                    <button type='button' autoFocus onClick={onStay}>Stay signed in</button>
                    <button type='button' className='button-secondary' onClick={onSignOut}>Sign out now</button>
                </div>
            </div>
        </DraggableDialog>
    );
}

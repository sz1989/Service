import { useActionState } from "react";
import type { NewPerson } from "../api/types";
import styles from "./AddPersonDialog.module.css";
import { DraggableDialog } from "./ui/DraggableDialog";

// Today as YYYY-MM-DD in the user's local time zone (toISOString() would give the UTC date).
function today(): string {
    return new Date().toLocaleDateString('en-CA')
}

interface PersonAddProps{
    onAdd: (person: NewPerson) => Promise<void>
    onCancel: () => void
}

export function AddPersonDialog({ onAdd, onCancel }: PersonAddProps) {
    // _prev is required by useActionState's signature but unused.
    async function addPersonAction(_prev: string | null, formData: FormData) : Promise<string | null> {
      const managerId = String(formData.get('managerId'))
      const person: NewPerson = {
        name: String(formData.get('name')),
        dateOfBirth: String(formData.get('dateOfBirth')),
        managerId: managerId === '' ? null : Number(managerId),
        salary: Number(formData.get('salary')),
      }

      try{
        await onAdd(person)
        return null
      } catch (err){
        return err instanceof Error ? err.message : 'Save failed.'
      }
    }

    const [error, formAction, isPending] = useActionState(addPersonAction, null)

    return (
        <DraggableDialog title='Add Person' onClose={onCancel}>
            <form className={styles.form} action={formAction}>
                <label htmlFor='person-name'>Name</label>
                <input id='person-name' name='name' type='text' required />

                <label htmlFor='person-dob'>Date of birth</label>
                <input id='person-dob' name='dateOfBirth' type='date' required defaultValue={today()} />

                <label htmlFor='person-manager'>Manager id</label>
                <input id='person-manager' name='managerId' type='number' />

                <label htmlFor='person-salary'>Salary</label>
                <input id='person-salary' name='salary' type='number' required min={0} />
                {error &&  <p role='alert' className={styles.error}>{error}</p>}
                <div className={styles.buttons}>
                    <button type='submit' disabled={isPending}>Ok</button>
                    <button type='button' onClick={onCancel}>Cancel</button>
                </div>
            </form>
        </DraggableDialog>
    );
}

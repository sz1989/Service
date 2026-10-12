import { useState, type SubmitEvent } from "react";
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
    const [isSaving, setIsSaving] = useState(false)
    const [error, setError] = useState<string | null>(null)

    async function handleSubmit(e: SubmitEvent<HTMLFormElement>){
      e.preventDefault()
      // The inputs are uncontrolled: read their values straight from the form (before any await).
      const data = new FormData(e.currentTarget)
      const managerId = String(data.get('managerId'))
      const person: NewPerson = {
        name: String(data.get('name')),
        dateOfBirth: String(data.get('dateOfBirth')),
        managerId: managerId === '' ? null : Number(managerId),
        salary: Number(data.get('salary')),
      }

      setIsSaving(true)
      setError(null)
      try{
        await onAdd(person)
      } catch (err){
        setError(err instanceof Error ? err.message : 'Save Failed.do')
        setIsSaving(false)
      }
    }

    return (
        <DraggableDialog title='Add Person' onClose={onCancel}>
            <form className={styles.form} onSubmit={handleSubmit}>
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
                    <button type='submit' disabled={isSaving}>Ok</button>
                    <button type='button' onClick={onCancel}>Cancel</button>
                </div>
            </form>
        </DraggableDialog>
    );
}

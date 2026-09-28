export interface LoginResponse {
  token: string
}

export interface Person {
  id: number
  name: string
  dateOfBirth: string
  managerId: number | null
  salary: number
}

export interface DocumentMatch {
  id: string
  text: string
  distance: number
}

export interface ChatAskAccepted {
  requestId: string
}

export interface ChatAnswerMessage {
  requestId: string
  question: string
  answer: string | null
}

export interface ChatErrorMessage {
  requestId: string
  message: string
}

export interface ChatCancelledMessage {
  requestId: string
}

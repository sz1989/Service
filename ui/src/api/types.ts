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

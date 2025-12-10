namespace StudentGradesSystem

type Role = Admin | Viewer

type User = {
    Username: string
    Password: string
    Role: Role
}

namespace StudentGradesSystem

type Role = Admin | Teacher | Viewer

type User = {
    Username: string
    Password: string
    Role: Role
}

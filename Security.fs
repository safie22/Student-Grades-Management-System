namespace StudentGradesSystem

open System
open System.IO
open Newtonsoft.Json

module Authentication =
    let private usersFile = "users.json"
    let private sessionsFile = "sessions.json"

    let private saveUsers (users: User list) =
        let json = JsonConvert.SerializeObject(users, Formatting.Indented)
        File.WriteAllText(usersFile, json)

    let private loadUsers() =
        if File.Exists(usersFile) then
            let json = File.ReadAllText(usersFile)
            let users = JsonConvert.DeserializeObject<User list>(json)
            if isNull (box users) then [] else users
        else
            let defaultUsers = [
                { Username = "admin"; Password = "admin123"; Role = Admin }
                { Username = "teacher"; Password = "teach123"; Role = Teacher }
                { Username = "viewer"; Password = "guest123"; Role = Viewer }
            ]
            saveUsers defaultUsers
            defaultUsers

    let private saveSessions (sessions: Map<string, User>) =
        let json = JsonConvert.SerializeObject(sessions, Formatting.Indented)
        File.WriteAllText(sessionsFile, json)

    let private loadSessions() =
        if File.Exists(sessionsFile) then
            let json = File.ReadAllText(sessionsFile)
            let sessions = JsonConvert.DeserializeObject<Map<string, User>>(json)
            if isNull (box sessions) then Map.empty else sessions
        else
            Map.empty

    let mutable private sessions = loadSessions()
    let mutable private users = loadUsers()

    let login (username: string) (password: string) =
        match users |> List.tryFind (fun u -> u.Username = username && u.Password = password) with
        | Some user ->
            let sessionId = Guid.NewGuid().ToString()
            sessions <- sessions.Add(sessionId, user)
            saveSessions sessions
            Ok sessionId
        | None ->
            Error "Invalid credentials"

    let logout (sessionId: string) =
        sessions <- sessions.Remove sessionId
        saveSessions sessions
        printfn "Logged out successfully."

    let getCurrentUser (sessionId: string) =
        sessions |> Map.tryFind sessionId

    let isLoggedIn (sessionId: string) =
        sessions.ContainsKey sessionId

    let createUser (currentUser: User) (newUsername: string) (password: string) (role: Role) =
        if currentUser.Role <> Admin then
            Error "Only Admin can create users."
        elif String.IsNullOrWhiteSpace(newUsername) then
            Error "Username cannot be empty."
        elif String.IsNullOrWhiteSpace(password) then
            Error "Password cannot be empty."
        elif users |> List.exists (fun u -> u.Username = newUsername) then
            Error "Username already exists."
        else
            let newUser = { Username = newUsername; Password = password; Role = role }
            users <- users @ [newUser]
            saveUsers users
            Ok ()

    let updateUser (currentUser: User) (usernameToUpdate: string) (newPassword: string option) (newRole: Role option) =
        if currentUser.Role <> Admin then
            Error "Only Admin can update users."
        else
            match users |> List.tryFind (fun u -> u.Username = usernameToUpdate) with
            | Some oldUser ->
                let updatedUser = { 
                    oldUser with 
                        Password = defaultArg newPassword oldUser.Password
                        Role = defaultArg newRole oldUser.Role
                }
                users <- (users |> List.filter (fun u -> u.Username <> usernameToUpdate)) @ [updatedUser]
                saveUsers users
                Ok ()
            | None ->
                Error "User not found."

    let deleteUser (currentUser: User) (usernameToDelete: string) =
        if currentUser.Role <> Admin then
            Error "Only Admin can delete users."
        else
            match users |> List.tryFind (fun u -> u.Username = usernameToDelete) with
            | Some _ ->
                users <- users |> List.filter (fun u -> u.Username <> usernameToDelete)
                saveUsers users
                Ok ()
            | None ->
                Error "User not found."

module Permissions =
    let canAddStudent (user: User) = user.Role = Admin
    let canEditStudent (user: User) = user.Role = Admin || user.Role = Teacher
    let canDeleteStudent (user: User) = user.Role = Admin
    
    let canAddGrades (user: User) = user.Role = Admin || user.Role = Teacher
    let canEditGrades (user: User) = user.Role = Admin || user.Role = Teacher
    
    let canViewStudents (user: User) = true
    let canViewGrades (user: User) = true
    let canViewAverages (user: User) = true
    let canViewStatistics (user: User) = true
    
    let canSaveData (user: User) = user.Role = Admin || user.Role = Teacher
    let canLoadData (user: User) = true

module AccessControl =
    let executeWithPermission (user: User) (permission: User -> bool) (operation: unit -> 'T) =
        if permission user then
            try
                Ok (operation ())
            with ex -> 
                Error (sprintf "Operation failed: %s" ex.Message)
        else
            Error "Access Denied"

    let requireAdmin (user: User) =
        if user.Role = Admin then Ok ()
        else Error "Only Admin can do that."

    let getRoleName (role: Role) =
        match role with
        | Admin -> "Administrator"
        | Teacher -> "Teacher"
        | Viewer -> "Viewer"

    let getUserPermissions (user: User) =
        match user.Role with
        | Admin -> 
            ["Add Student"; "Edit Student"; "Delete Student"; 
             "Add Grades"; "Edit Grades";
             "View Students"; "View Grades"; "View Averages"; 
             "View Statistics"; "Save Data"; "Load Data"]
        | Teacher ->
            ["Edit Student"; "Add Grades"; "Edit Grades";
             "View Students"; "View Grades"; "View Averages"; 
             "View Statistics"; "Save Data"; "Load Data"]
        | Viewer -> 
            ["View Students"; "View Grades"; "View Averages"; 
             "View Statistics"; "Load Data"]

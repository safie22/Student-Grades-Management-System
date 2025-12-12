open StudentGradesSystem

[<EntryPoint>]
let main argv =
    printfn "=== Student Grades Management System ==="
    printfn ""
    
    // Test Login
    printfn "Testing Login..."
    match Authentication.login "admin" "admin123" with
    | Ok sessionId ->
        printfn "Login successful! Session: %s" (sessionId.Substring(0, 8) + "...")
        
        match Authentication.getCurrentUser sessionId with
        | Some user ->
            printfn "Logged in as: %s (%s)" user.Username (AccessControl.getRoleName user.Role)
            printfn ""
            
            // Show permissions
            printfn "Your permissions:"
            AccessControl.getUserPermissions user
            |> List.iter (fun p -> printfn "  - %s" p)
            printfn ""
            
            // Test creating a new user
            printfn "Creating new user 'teacher'..."
            match Authentication.createUser user "teacher" "teach123" Viewer with
            | Ok () -> printfn "User created successfully!"
            | Error msg -> printfn "Error: %s" msg
            
            printfn ""
            
            // Test permission check
            printfn "Testing permissions..."
            printfn "Can add student: %b" (Permissions.canAddStudent user)
            printfn "Can view grades: %b" (Permissions.canViewGrades user)
            
            // Logout
            printfn ""
            Authentication.logout sessionId
            
        | None ->
            printfn "Error: Could not get user info"
            
    | Error msg ->
        printfn "Login failed: %s" msg
    
    printfn ""
    printfn "Press any key to exit..."
    System.Console.ReadKey() |> ignore
    0

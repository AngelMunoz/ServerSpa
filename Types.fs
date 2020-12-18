namespace ServerSpa

[<CLIMutable>]
type LoginPayload = { email: string; password: string }

[<CLIMutable>]
type SignupPayload =
    { name: string
      email: string
      password: string }

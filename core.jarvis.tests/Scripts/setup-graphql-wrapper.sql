-- This script must be run as supabase_admin user to create the wrapper function
-- It creates a wrapper function that allows postgres user to execute GraphQL queries

-- Create the wrapper function with SECURITY DEFINER to run with supabase_admin privileges
CREATE OR REPLACE FUNCTION public.graphql_resolve(query text)
RETURNS json
LANGUAGE sql
SECURITY DEFINER
SET search_path = graphql, public
AS $function$
    SELECT graphql.resolve($1);
$function$;

-- Change the owner to supabase_admin to ensure it has proper permissions
ALTER FUNCTION public.graphql_resolve(text) OWNER TO supabase_admin;

-- Grant execute permission on the wrapper function to postgres
GRANT EXECUTE ON FUNCTION public.graphql_resolve(text) TO postgres;

-- Log success
DO $$
BEGIN
    RAISE NOTICE 'Successfully created public.graphql_resolve wrapper function';
END $$;
import { useState, type FormEvent } from 'react';
import { usersApi } from '@/api/endpoints';
import {
  EmptyState,
  ErrorMessage,
  LoadingIndicator,
  SuccessMessage,
} from '@/components/Feedback';
import { EditIcon, PlusIcon, TrashIcon } from '@/components/Icons';
import { ConfirmationDialog, Modal } from '@/components/Modal';
import { useAuthentication } from '@/hooks/useAuthentication';
import { useAsyncData } from '@/hooks/useAsyncData';
import { formatDateTime } from '@/utils/formatters';
import { buildClickableRowProps } from '@/utils/tableInteraction';
import type { Role, User } from '@/types/api';

const MINIMUM_PASSWORD_LENGTH = 8;

/** Valores del formulario de usuario. */
interface UserFormValues {
  userName: string;
  email: string;
  fullName: string;
  password: string;
  isActive: boolean;
  roleIdentifiers: string[];
}

const EMPTY_FORM_VALUES: UserFormValues = {
  userName: '',
  email: '',
  fullName: '',
  password: '',
  isActive: true,
  roleIdentifiers: [],
};

/**
 * Administracion de usuarios y roles. Solo visible para el rol administrador,
 * tanto en el menu como en la API.
 */
export function UsersPage() {
  const { session } = useAuthentication();

  const rolesQuery = useAsyncData<Role[]>(() => usersApi.getRoles(), []);
  const usersQuery = useAsyncData<User[]>(() => usersApi.getAll(), []);

  const [isCreating, setIsCreating] = useState(false);
  const [userBeingEdited, setUserBeingEdited] = useState<User | null>(null);
  const [userBeingDeleted, setUserBeingDeleted] = useState<User | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [operationError, setOperationError] = useState<unknown>(null);
  const [isProcessing, setIsProcessing] = useState(false);

  function resetMessages() {
    setSuccessMessage(null);
    setOperationError(null);
  }

  /**
   * Abre el formulario de edicion. Se extrae a una funcion porque la accion se
   * dispara desde dos lugares: el boton de la fila y el clic sobre la fila.
   */
  function openEditor(user: User) {
    resetMessages();
    setUserBeingEdited(user);
  }

  function openDeleteConfirmation(user: User) {
    resetMessages();
    setUserBeingDeleted(user);
  }

  async function handleCreate(values: UserFormValues) {
    setIsProcessing(true);
    setOperationError(null);

    try {
      const createdUser = await usersApi.create({
        userName: values.userName.trim(),
        email: values.email.trim(),
        fullName: values.fullName.trim(),
        password: values.password,
        roleIdentifiers: values.roleIdentifiers,
      });

      setIsCreating(false);
      setSuccessMessage(`Usuario ${createdUser.userName} creado correctamente.`);
      await usersQuery.reload();
    } catch (error) {
      setOperationError(error);
    } finally {
      setIsProcessing(false);
    }
  }

  async function handleUpdate(values: UserFormValues) {
    if (!userBeingEdited) {
      return;
    }

    setIsProcessing(true);
    setOperationError(null);

    try {
      await usersApi.update(userBeingEdited.id, {
        email: values.email.trim(),
        fullName: values.fullName.trim(),
        isActive: values.isActive,
        roleIdentifiers: values.roleIdentifiers,
      });

      setUserBeingEdited(null);
      setSuccessMessage(`Usuario ${values.userName} actualizado correctamente.`);
      await usersQuery.reload();
    } catch (error) {
      setOperationError(error);
    } finally {
      setIsProcessing(false);
    }
  }

  async function handleDelete() {
    if (!userBeingDeleted) {
      return;
    }

    setIsProcessing(true);
    setOperationError(null);

    try {
      await usersApi.remove(userBeingDeleted.id);
      setSuccessMessage(`Usuario ${userBeingDeleted.userName} eliminado.`);
      setUserBeingDeleted(null);
      await usersQuery.reload();
    } catch (error) {
      setOperationError(error);
      setUserBeingDeleted(null);
    } finally {
      setIsProcessing(false);
    }
  }

  return (
    <>
      <section className="card">
        <div className="card__header">
          <div>
            <h2 className="card__title">Usuarios registrados</h2>
            <p className="card__description">
              Los roles determinan que modulos puede usar cada usuario.
            </p>
          </div>
          <button
            type="button"
            className="button button--accent"
            onClick={() => {
              resetMessages();
              setIsCreating(true);
            }}
          >
            <PlusIcon size={16} /> Nuevo usuario
          </button>
        </div>

        <SuccessMessage message={successMessage} />
        <ErrorMessage error={operationError ?? usersQuery.error} />

        {usersQuery.isLoading && <LoadingIndicator />}

        {!usersQuery.isLoading && usersQuery.data && (
          <>
            {usersQuery.data.length === 0 ? (
              <EmptyState title="No hay usuarios registrados" />
            ) : (
              <div className="table-wrapper">
                <table className="table">
                  <thead>
                    <tr>
                      <th>Usuario</th>
                      <th>Nombre completo</th>
                      <th>Correo</th>
                      <th>Roles</th>
                      <th>Estado</th>
                      <th>Ultimo acceso</th>
                      <th aria-label="Acciones" />
                    </tr>
                  </thead>
                  <tbody>
                    {usersQuery.data.map((user) => (
                      <tr
                        key={user.id}
                        {...buildClickableRowProps(
                          () => openEditor(user),
                          `Editar ${user.userName}`,
                        )}
                      >
                        <td>{user.userName}</td>
                        <td>{user.fullName}</td>
                        <td>{user.email}</td>
                        <td>
                          {user.roles.map((role) => (
                            <span
                              className="badge badge--role"
                              key={role.id}
                              style={{ marginRight: 4 }}
                            >
                              {role.name}
                            </span>
                          ))}
                        </td>
                        <td>
                          <span
                            className={
                              user.isActive ? 'badge badge--active' : 'badge badge--inactive'
                            }
                          >
                            {user.isActive ? 'Activo' : 'Inactivo'}
                          </span>
                        </td>
                        <td>{formatDateTime(user.lastLoginAt)}</td>
                        <td>
                          <div className="table__actions">
                            <button
                              type="button"
                              className="button button--icon"
                              title="Editar"
                              aria-label={`Editar ${user.userName}`}
                              onClick={() => openEditor(user)}
                            >
                              <EditIcon />
                            </button>
                            <button
                              type="button"
                              className="button button--icon"
                              title={
                                user.id === session?.userId
                                  ? 'No puede eliminar su propia cuenta'
                                  : 'Eliminar'
                              }
                              aria-label={`Eliminar ${user.userName}`}
                              disabled={user.id === session?.userId}
                              onClick={() => openDeleteConfirmation(user)}
                            >
                              <TrashIcon />
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </>
        )}
      </section>

      <Modal
        title="Nuevo usuario"
        description="La contrasena debe tener mayuscula, minuscula y numero."
        isOpen={isCreating}
        onClose={() => setIsCreating(false)}
      >
        <UserForm
          roles={rolesQuery.data ?? []}
          isSubmitting={isProcessing}
          submitError={operationError}
          onSubmit={handleCreate}
          onCancel={() => setIsCreating(false)}
        />
      </Modal>

      <Modal
        title="Editar usuario"
        isOpen={userBeingEdited !== null}
        onClose={() => setUserBeingEdited(null)}
      >
        {userBeingEdited && (
          <UserForm
            user={userBeingEdited}
            roles={rolesQuery.data ?? []}
            isSubmitting={isProcessing}
            submitError={operationError}
            onSubmit={handleUpdate}
            onCancel={() => setUserBeingEdited(null)}
          />
        )}
      </Modal>

      <ConfirmationDialog
        isOpen={userBeingDeleted !== null}
        title="Eliminar usuario"
        message={`Se eliminara el usuario ${userBeingDeleted?.userName ?? ''}. Esta accion no se puede deshacer.`}
        isProcessing={isProcessing}
        onConfirm={handleDelete}
        onCancel={() => setUserBeingDeleted(null)}
      />
    </>
  );
}

function UserForm({
  user,
  roles,
  isSubmitting,
  submitError,
  onSubmit,
  onCancel,
}: {
  user?: User | null;
  roles: Role[];
  isSubmitting: boolean;
  submitError: unknown;
  onSubmit: (values: UserFormValues) => Promise<void>;
  onCancel: () => void;
}) {
  const isEditing = user !== null && user !== undefined;

  const [values, setValues] = useState<UserFormValues>(
    user
      ? {
          userName: user.userName,
          email: user.email,
          fullName: user.fullName,
          password: '',
          isActive: user.isActive,
          roleIdentifiers: user.roles.map((role) => role.id),
        }
      : EMPTY_FORM_VALUES,
  );

  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

  function toggleRole(roleId: string) {
    setValues((previousValues) => ({
      ...previousValues,
      roleIdentifiers: previousValues.roleIdentifiers.includes(roleId)
        ? previousValues.roleIdentifiers.filter((identifier) => identifier !== roleId)
        : [...previousValues.roleIdentifiers, roleId],
    }));
    setValidationErrors((previousErrors) => ({ ...previousErrors, roleIdentifiers: '' }));
  }

  async function handleSubmit(formEvent: FormEvent<HTMLFormElement>) {
    formEvent.preventDefault();

    const errors: Record<string, string> = {};

    if (!isEditing && values.userName.trim().length < 4) {
      errors.userName = 'El usuario debe tener al menos 4 caracteres.';
    }

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.email.trim())) {
      errors.email = 'Capture un correo electronico valido.';
    }

    if (values.fullName.trim().length === 0) {
      errors.fullName = 'El nombre completo es obligatorio.';
    }

    if (!isEditing && values.password.length < MINIMUM_PASSWORD_LENGTH) {
      errors.password = `La contrasena debe tener al menos ${MINIMUM_PASSWORD_LENGTH} caracteres.`;
    }

    if (values.roleIdentifiers.length === 0) {
      errors.roleIdentifiers = 'Asigne al menos un rol.';
    }

    if (Object.keys(errors).length > 0) {
      setValidationErrors(errors);

      return;
    }

    await onSubmit(values);
  }

  return (
    <form onSubmit={handleSubmit} noValidate>
      <ErrorMessage error={submitError} />

      <div className="form-grid">
        <div className="field">
          <label className="field__label" htmlFor="userName">
            Nombre de usuario
          </label>
          <input
            id="userName"
            className={`control${validationErrors.userName ? ' control--invalid' : ''}`}
            type="text"
            value={values.userName}
            disabled={isEditing}
            onChange={(changeEvent) =>
              setValues({ ...values, userName: changeEvent.target.value })
            }
          />
          {validationErrors.userName && (
            <span className="field__error">{validationErrors.userName}</span>
          )}
        </div>

        <div className="field">
          <label className="field__label" htmlFor="userEmail">
            Correo electronico
          </label>
          <input
            id="userEmail"
            className={`control${validationErrors.email ? ' control--invalid' : ''}`}
            type="email"
            value={values.email}
            onChange={(changeEvent) =>
              setValues({ ...values, email: changeEvent.target.value })
            }
          />
          {validationErrors.email && (
            <span className="field__error">{validationErrors.email}</span>
          )}
        </div>

        <div className="field">
          <label className="field__label" htmlFor="userFullName">
            Nombre completo
          </label>
          <input
            id="userFullName"
            className={`control${validationErrors.fullName ? ' control--invalid' : ''}`}
            type="text"
            value={values.fullName}
            onChange={(changeEvent) =>
              setValues({ ...values, fullName: changeEvent.target.value })
            }
          />
          {validationErrors.fullName && (
            <span className="field__error">{validationErrors.fullName}</span>
          )}
        </div>

        {!isEditing && (
          <div className="field">
            <label className="field__label" htmlFor="userPassword">
              Contrasena inicial
            </label>
            <input
              id="userPassword"
              className={`control${validationErrors.password ? ' control--invalid' : ''}`}
              type="password"
              autoComplete="new-password"
              value={values.password}
              onChange={(changeEvent) =>
                setValues({ ...values, password: changeEvent.target.value })
              }
            />
            <span className="field__hint">
              Minimo {MINIMUM_PASSWORD_LENGTH} caracteres, con mayuscula, minuscula y numero.
            </span>
            {validationErrors.password && (
              <span className="field__error">{validationErrors.password}</span>
            )}
          </div>
        )}

        {isEditing && (
          <div className="field">
            <label className="field__label" htmlFor="userIsActive">
              Estado
            </label>
            <select
              id="userIsActive"
              className="control"
              value={values.isActive ? 'true' : 'false'}
              onChange={(changeEvent) =>
                setValues({ ...values, isActive: changeEvent.target.value === 'true' })
              }
            >
              <option value="true">Activo</option>
              <option value="false">Inactivo</option>
            </select>
          </div>
        )}
      </div>

      <div style={{ marginTop: 16 }}>
        <p className="section-title">Roles asignados</p>
        {roles.map((role) => (
          <label
            key={role.id}
            className="detail-row"
            style={{ cursor: 'pointer', alignItems: 'flex-start' }}
          >
            <span>
              <input
                type="checkbox"
                checked={values.roleIdentifiers.includes(role.id)}
                onChange={() => toggleRole(role.id)}
                style={{ marginRight: 8 }}
              />
              <strong>{role.name}</strong>
              <br />
              <span className="field__hint">{role.description}</span>
            </span>
          </label>
        ))}
        {validationErrors.roleIdentifiers && (
          <span className="field__error">{validationErrors.roleIdentifiers}</span>
        )}
      </div>

      <div className="form-actions">
        <button
          type="button"
          className="button button--secondary"
          onClick={onCancel}
          disabled={isSubmitting}
        >
          Cancelar
        </button>
        <button type="submit" className="button button--primary" disabled={isSubmitting}>
          {isSubmitting ? 'Guardando...' : isEditing ? 'Guardar cambios' : 'Crear usuario'}
        </button>
      </div>
    </form>
  );
}
